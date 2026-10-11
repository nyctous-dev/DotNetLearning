using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using BoidsSimulation.Core.Models;
namespace BoidsSimulation.Core.Simulation;
public readonly record struct SimulationStepTimings(
    TimeSpan PopulationPreparation,
    TimeSpan GridRebuild,
    TimeSpan SteeringCalculation,
    TimeSpan VelocityCommit,
    TimeSpan PositionUpdate,
    TimeSpan TotalDuration)
{
    public static SimulationStepTimings Empty => new(
        TimeSpan.Zero,
        TimeSpan.Zero,
        TimeSpan.Zero,
        TimeSpan.Zero,
        TimeSpan.Zero,
        TimeSpan.Zero);
}
public readonly record struct SimulationStepWorkMetrics(
    int BoidCount,
    long GridCandidatesReturned,
    long GridCellsVisited,
    long GridOccupiedCellsVisited,
    long GridLinkedListEntriesTraversed,
    long NeighborCandidatesExamined,
    long SquaredDistanceCalculations,
    long SeparationMatches,
    long AlignmentMatches,
    long CohesionMatches,
    int BoidsWithZeroEvaluatedNeighbors,
    int BoidsAtEvaluationLimit,
    int MinimumEvaluationsPerBoid,
    double AverageEvaluationsPerBoid,
    int MedianEvaluationsPerBoid,
    int P95EvaluationsPerBoid,
    int MaximumEvaluationsPerBoid)
{
    public static SimulationStepWorkMetrics Empty => new(
        BoidCount: 0,
        GridCandidatesReturned: 0L,
        GridCellsVisited: 0L,
        GridOccupiedCellsVisited: 0L,
        GridLinkedListEntriesTraversed: 0L,
        NeighborCandidatesExamined: 0L,
        SquaredDistanceCalculations: 0L,
        SeparationMatches: 0L,
        AlignmentMatches: 0L,
        CohesionMatches: 0L,
        BoidsWithZeroEvaluatedNeighbors: 0,
        BoidsAtEvaluationLimit: 0,
        MinimumEvaluationsPerBoid: 0,
        AverageEvaluationsPerBoid: 0.0,
        MedianEvaluationsPerBoid: 0,
        P95EvaluationsPerBoid: 0,
        MaximumEvaluationsPerBoid: 0);
}
public sealed class SimulationStepper
{
    private const float Epsilon = 1e-6f;
    private const int ChunksPerProcessor = 4;
    private readonly object _settingsSyncRoot = new();
    private readonly SimulationLimits _limits;
    private SimulationSettings _settings;

    private readonly BoidSpatialGrid _grid;
    private readonly int _parallelChunkCapacity;
    private readonly int[][] _candidateBuffers;
    private readonly SteeringWorkAccumulator[] _chunkWork;

    private float _separationRadiusSquared;
    private float _alignmentRadiusSquared;
    private float _cohesionRadiusSquared;
    private float _searchRadius;
    private float _searchRadiusSquared;
    private float _interactionRadiusSquared;
    private readonly int[] _evaluationHistogram;
    private Boid[] _populationBuffer = Array.Empty<Boid>();
    private Vector2[] _nextVelocities = Array.Empty<Vector2>();
    private int[] _evaluationsPerBoid = Array.Empty<int>();
    private struct SteeringWorkAccumulator
    {
        public long GridCandidatesReturned;
        public long GridCellsVisited;
        public long GridOccupiedCellsVisited;
        public long GridLinkedListEntriesTraversed;
        public long NeighborCandidatesExamined;
        public long SquaredDistanceCalculations;
        public long SeparationMatches;
        public long AlignmentMatches;
        public long CohesionMatches;
        public int BoidsWithZeroEvaluatedNeighbors;
        public int BoidsAtEvaluationLimit;
        public void Add(in SteeringWorkAccumulator other)
        {
            GridCandidatesReturned += other.GridCandidatesReturned;
            GridCellsVisited += other.GridCellsVisited;
            GridOccupiedCellsVisited += other.GridOccupiedCellsVisited;
            GridLinkedListEntriesTraversed += other.GridLinkedListEntriesTraversed;
            NeighborCandidatesExamined += other.NeighborCandidatesExamined;
            SquaredDistanceCalculations += other.SquaredDistanceCalculations;
            SeparationMatches += other.SeparationMatches;
            AlignmentMatches += other.AlignmentMatches;
            CohesionMatches += other.CohesionMatches;
            BoidsWithZeroEvaluatedNeighbors += other.BoidsWithZeroEvaluatedNeighbors;
            BoidsAtEvaluationLimit += other.BoidsAtEvaluationLimit;
        }
    }
    public SimulationStepTimings LastStepTimings { get; private set; } =
        SimulationStepTimings.Empty;
    public SimulationStepWorkMetrics LastStepWorkMetrics { get; private set; } =
        SimulationStepWorkMetrics.Empty;
    public SimulationStepper(
        SimulationSettings settings,
        SimulationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(limits);
        settings.Validate(limits);
        _limits = limits;
        _settings = settings;
        RecalculateDerivedSettings(settings);
        // More chunks than processors let the scheduler balance uneven boid
        // workloads without sharing per-chunk scratch buffers or counters.
        _parallelChunkCapacity =
            Math.Max(1, Environment.ProcessorCount) * ChunksPerProcessor;
        _candidateBuffers = new int[_parallelChunkCapacity][];
        _chunkWork = new SteeringWorkAccumulator[_parallelChunkCapacity];
        for (var chunkIndex = 0;
             chunkIndex < _candidateBuffers.Length;
             chunkIndex++)
        {
            _candidateBuffers[chunkIndex] =
                new int[settings.MaxNeighborEvaluationsPerBoid + 1];
        }
        _evaluationHistogram =
            new int[settings.MaxNeighborEvaluationsPerBoid + 1];
        _grid = new BoidSpatialGrid(
            settings.WorldWidth,
            settings.WorldHeight,
            settings.CellSize,
            Array.Empty<Boid>());
    }
    public void UpdateBehaviorSettings(SimulationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        // Validate every setting against the configured application limits.
        settings.Validate(_limits);

        lock (_settingsSyncRoot)
        {
            // These settings affect the grid, allocated buffers, population
            // management, or simulation timer. They cannot change through this
            // behavior-settings update method.
            if (settings.WorldWidth != _settings.WorldWidth ||
                settings.WorldHeight != _settings.WorldHeight ||
                settings.MaxBoids != _settings.MaxBoids ||
                settings.InitialBoidCount != _settings.InitialBoidCount ||
                settings.CellSize != _settings.CellSize ||
                settings.FixedTimeStep != _settings.FixedTimeStep ||
                settings.MaxNeighborEvaluationsPerBoid !=
                    _settings.MaxNeighborEvaluationsPerBoid)
            {
                throw new ArgumentException(
                    "Runtime behavior updates cannot change world dimensions, " +
                    "population limits, cell size, time step, or the neighbor " +
                    "evaluation limit.",
                    nameof(settings));
            }

            _settings = settings;
            RecalculateDerivedSettings(settings);
        }
    }

    public void Step(SimulationState state)
    {
        Step(state, Array.Empty<BoidInteraction>());
    }

    public void Step(
        SimulationState state,
        IReadOnlyList<BoidInteraction> interactions)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(interactions);

        lock (_settingsSyncRoot)
        {
            StepCore(state, interactions);
        }
    }

    private void StepCore(
        SimulationState state,
        IReadOnlyList<BoidInteraction> interactions)
    {
        var totalStart = Stopwatch.GetTimestamp();
        var populationCount = state.BoidCount;
        if (populationCount == 0)
        {
            LastStepTimings = SimulationStepTimings.Empty;
            LastStepWorkMetrics = SimulationStepWorkMetrics.Empty;
            return;
        }
        // Phase 1: Prepare reusable buffers and population ordering.
        var phaseStart = Stopwatch.GetTimestamp();
        EnsureCapacity(populationCount);
        Array.Clear(
            _evaluationHistogram,
            0,
            _evaluationHistogram.Length);
        var populationIndex = 0;
        foreach (var boid in state.Boids)
        {
            _populationBuffer[populationIndex++] = boid;
        }
        var populationPreparationDuration =
            Stopwatch.GetElapsedTime(phaseStart);
        // Phase 2: Build the spatial grid for the current positions.
        phaseStart = Stopwatch.GetTimestamp();
        _grid.Rebuild(_populationBuffer);
        var gridRebuildDuration =
            Stopwatch.GetElapsedTime(phaseStart);
        // Phase 3: Calculate every next velocity before mutating any boid.
        // Each small range owns its candidate buffer and work accumulator.
        // More ranges than processors allow the scheduler to balance uneven
        // workloads while preserving a read-only population snapshot.
        phaseStart = Stopwatch.GetTimestamp();
        var chunkCount = Math.Min(_parallelChunkCapacity, populationCount);
        Parallel.For(0, chunkCount, chunkIndex =>
        {
            var localWork = new SteeringWorkAccumulator();
            var candidateBuffer = _candidateBuffers[chunkIndex];
            var rangeStart = (int)((long)populationCount * chunkIndex / chunkCount);
            var rangeEnd = (int)((long)populationCount * (chunkIndex + 1) / chunkCount);
            for (var i = rangeStart; i < rangeEnd; i++)
            {
                var boid = _populationBuffer[i];
                var steering = CalculateSteering(
                    i,
                    boid,
                    candidateBuffer,
                    ref localWork,
                    out var evaluationsForBoid);
                steering += CalculateInteractionSteering(boid, interactions);
                _evaluationsPerBoid[i] = evaluationsForBoid;
                if (steering == Vector2.Zero)
                {
                    _nextVelocities[i] = boid.Velocity;
                    continue;
                }
                steering = ClampMagnitude(
                    steering,
                    _settings.MaxSteeringForce);
                var nextVelocity =
                    boid.Velocity +
                    steering * _settings.FixedTimeStep;
                _nextVelocities[i] = ClampMagnitude(
                    nextVelocity,
                    _settings.MaxSpeed);
            }
            _chunkWork[chunkIndex] = localWork;
        });
        var work = new SteeringWorkAccumulator();
        for (var chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
        {
            work.Add(_chunkWork[chunkIndex]);
        }
        // Build the evaluation distribution after parallel work. This avoids
        // shared histogram increments and keeps the percentile calculation exact.
        var minimumEvaluations = int.MaxValue;
        var maximumEvaluations = 0;
        for (var i = 0; i < populationCount; i++)
        {
            var evaluationsForBoid = _evaluationsPerBoid[i];
            _evaluationHistogram[evaluationsForBoid]++;
            if (evaluationsForBoid < minimumEvaluations)
            {
                minimumEvaluations = evaluationsForBoid;
            }
            if (evaluationsForBoid > maximumEvaluations)
            {
                maximumEvaluations = evaluationsForBoid;
            }
            if (evaluationsForBoid == 0)
            {
                work.BoidsWithZeroEvaluatedNeighbors++;
            }
            if (evaluationsForBoid >= _settings.MaxNeighborEvaluationsPerBoid)
            {
                work.BoidsAtEvaluationLimit++;
            }
        }
        var steeringCalculationDuration =
            Stopwatch.GetElapsedTime(phaseStart);
        LastStepWorkMetrics = CreateWorkMetrics(
            populationCount,
            work,
            minimumEvaluations,
            maximumEvaluations);
        // Phase 4: Commit the calculated velocities.
        phaseStart = Stopwatch.GetTimestamp();
        for (var i = 0; i < populationCount; i++)
        {
            _populationBuffer[i].Velocity = _nextVelocities[i];
        }
        var velocityCommitDuration =
            Stopwatch.GetElapsedTime(phaseStart);
        // Phase 5: Advance positions and wrap them around the world.
        phaseStart = Stopwatch.GetTimestamp();
        var worldWidth = (float)_settings.WorldWidth;
        var worldHeight = (float)_settings.WorldHeight;
        var halfWidth = worldWidth / 2f;
        var halfHeight = worldHeight / 2f;
        var timeStep = _settings.FixedTimeStep;
        for (var i = 0; i < populationCount; i++)
        {
            var boid = _populationBuffer[i];
            var nextPosition =
                boid.Position + boid.Velocity * timeStep;
            boid.Position = new Vector2(
                WrapCoordinate(
                    nextPosition.X,
                    worldWidth,
                    halfWidth),
                WrapCoordinate(
                    nextPosition.Y,
                    worldHeight,
                    halfHeight));
        }
        var positionUpdateDuration =
            Stopwatch.GetElapsedTime(phaseStart);
        var totalDuration =
            Stopwatch.GetElapsedTime(totalStart);
        LastStepTimings = new SimulationStepTimings(
            populationPreparationDuration,
            gridRebuildDuration,
            steeringCalculationDuration,
            velocityCommitDuration,
            positionUpdateDuration,
            totalDuration);
    }
    private void RecalculateDerivedSettings(SimulationSettings settings)
    {
        _separationRadiusSquared =
            settings.SeparationRadius * settings.SeparationRadius;

        _alignmentRadiusSquared =
            settings.AlignmentRadius * settings.AlignmentRadius;

        _cohesionRadiusSquared =
            settings.CohesionRadius * settings.CohesionRadius;

        _searchRadius = MathF.Max(
            settings.SeparationRadius,
            MathF.Max(
                settings.AlignmentRadius,
                settings.CohesionRadius));

        _searchRadiusSquared = _searchRadius * _searchRadius;

        _interactionRadiusSquared =
            settings.InteractionRadius * settings.InteractionRadius;
    }

    private Vector2 CalculateSteering(
        int boidIndex,
        Boid boid,
        int[] candidateBuffer,
        ref SteeringWorkAccumulator work,
        out int neighborEvaluations)
    {
        neighborEvaluations = 0;
        if (_searchRadius <= 0f)
        {
            return Vector2.Zero;
        }
        var candidateCount = _grid.QueryNeighborhoodIndices(
            boid.Position,
            _searchRadius,
            candidateBuffer.AsSpan(),
            out var gridQueryMetrics);
        work.GridCandidatesReturned += gridQueryMetrics.CandidatesReturned;
        work.GridCellsVisited += gridQueryMetrics.CellsVisited;
        work.GridOccupiedCellsVisited += gridQueryMetrics.OccupiedCellsVisited;
        work.GridLinkedListEntriesTraversed +=
            gridQueryMetrics.LinkedListEntriesTraversed;
        var separationSum = Vector2.Zero;
        var separationCount = 0;
        var alignmentVelocitySum = Vector2.Zero;
        var alignmentCount = 0;
        var cohesionOffsetSum = Vector2.Zero;
        var cohesionCount = 0;
        var maxEvaluations =
            _settings.MaxNeighborEvaluationsPerBoid;
        for (var i = 0; i < candidateCount; i++)
        {
            var candidateIndex = candidateBuffer[i];
            // Grid queries return population indices, not boid IDs.
            if (candidateIndex == boidIndex)
            {
                continue;
            }
            if (neighborEvaluations >= maxEvaluations)
            {
                break;
            }
            neighborEvaluations++;
            work.NeighborCandidatesExamined++;
            var neighbor = _populationBuffer[candidateIndex];
            var offset = GetWrappedOffset(
                boid.Position,
                neighbor.Position);
            // Reject candidates outside the largest influence radius.
            if (MathF.Abs(offset.X) > _searchRadius ||
                MathF.Abs(offset.Y) > _searchRadius)
            {
                continue;
            }
            work.SquaredDistanceCalculations++;
            var distanceSquared = offset.LengthSquared();
            // All steering rules use radii no larger than searchRadius.
            if (distanceSquared > _searchRadiusSquared)
            {
                continue;
            }
            if (_separationRadiusSquared > 0f &&
                distanceSquared > Epsilon &&
                distanceSquared <= _separationRadiusSquared)
            {
                // Give closer boids greater influence on separation.
                var inverseDistanceSquared = 1f / distanceSquared;
                separationSum -= offset * inverseDistanceSquared;
                separationCount++;
                work.SeparationMatches++;
            }
            if (_alignmentRadiusSquared > 0f &&
                distanceSquared <= _alignmentRadiusSquared)
            {
                alignmentVelocitySum += neighbor.Velocity;
                alignmentCount++;
                work.AlignmentMatches++;
            }
            if (_cohesionRadiusSquared > 0f &&
                distanceSquared <= _cohesionRadiusSquared)
            {
                cohesionOffsetSum += offset;
                cohesionCount++;
                work.CohesionMatches++;
            }
        }
        var separation = CalculateSeparationSteering(
            boid,
            separationSum,
            separationCount);
        var alignment = CalculateAlignmentSteering(
            boid,
            alignmentVelocitySum,
            alignmentCount);
        var cohesion = CalculateCohesionSteering(
            boid,
            cohesionOffsetSum,
            cohesionCount);
        return
            separation * _settings.SeparationWeight +
            alignment * _settings.AlignmentWeight +
            cohesion * _settings.CohesionWeight;
    }
    private Vector2 CalculateInteractionSteering(
        Boid boid,
        IReadOnlyList<BoidInteraction> interactions)
    {
        if (interactions.Count == 0 || _interactionRadiusSquared <= 0f)
        {
            return Vector2.Zero;
        }
        var totalSteering = Vector2.Zero;
        var interactionRadius = _settings.InteractionRadius;
        for (var i = 0; i < interactions.Count; i++)
        {
            var interaction = interactions[i];
            // Use the shortest wrapped offset so interactions work across
            // the world boundary in the toroidal simulation.
            var offset = GetWrappedOffset(boid.Position, interaction.Position);
            var distanceSquared = offset.LengthSquared();
            if (distanceSquared <= Epsilon ||
                distanceSquared > _interactionRadiusSquared)
            {
                continue;
            }
            var distance = MathF.Sqrt(distanceSquared);
            var falloff = 1f - distance / interactionRadius;
            var direction = offset / distance;
            totalSteering += interaction.Mode switch
            {
                BoidInteractionMode.Attract =>
                    direction * (_settings.AttractionForce * falloff),
                BoidInteractionMode.Repel =>
                    -direction * (_settings.RepulsionForce * falloff),
                _ => Vector2.Zero
            };
        }
        return totalSteering;
    }
    private SimulationStepWorkMetrics CreateWorkMetrics(
        int populationCount,
        SteeringWorkAccumulator work,
        int minimumEvaluations,
        int maximumEvaluations)
    {
        return new SimulationStepWorkMetrics(
            BoidCount: populationCount,
            GridCandidatesReturned: work.GridCandidatesReturned,
            GridCellsVisited: work.GridCellsVisited,
            GridOccupiedCellsVisited: work.GridOccupiedCellsVisited,
            GridLinkedListEntriesTraversed: work.GridLinkedListEntriesTraversed,
            NeighborCandidatesExamined: work.NeighborCandidatesExamined,
            SquaredDistanceCalculations: work.SquaredDistanceCalculations,
            SeparationMatches: work.SeparationMatches,
            AlignmentMatches: work.AlignmentMatches,
            CohesionMatches: work.CohesionMatches,
            BoidsWithZeroEvaluatedNeighbors:
                work.BoidsWithZeroEvaluatedNeighbors,
            BoidsAtEvaluationLimit: work.BoidsAtEvaluationLimit,
            MinimumEvaluationsPerBoid: minimumEvaluations,
            AverageEvaluationsPerBoid:
                (double)work.NeighborCandidatesExamined / populationCount,
            MedianEvaluationsPerBoid:
                FindNearestRankPercentile(populationCount, 0.50),
            P95EvaluationsPerBoid:
                FindNearestRankPercentile(populationCount, 0.95),
            MaximumEvaluationsPerBoid: maximumEvaluations);
    }
    private int FindNearestRankPercentile(
        int sampleCount,
        double percentile)
    {
        var targetRank = (int)Math.Ceiling(sampleCount * percentile);
        var cumulativeCount = 0;
        for (var evaluations = 0;
             evaluations < _evaluationHistogram.Length;
             evaluations++)
        {
            cumulativeCount += _evaluationHistogram[evaluations];
            if (cumulativeCount >= targetRank)
            {
                return evaluations;
            }
        }
        return _evaluationHistogram.Length - 1;
    }
    private Vector2 CalculateSeparationSteering(
        Boid boid,
        Vector2 separationSum,
        int neighborCount)
    {
        if (neighborCount == 0)
        {
            return Vector2.Zero;
        }
        var count = (float)neighborCount;
        if (separationSum.LengthSquared() <=
            Epsilon * count * count)
        {
            return Vector2.Zero;
        }
        var desiredVelocity =
            Vector2.Normalize(separationSum) *
            _settings.MaxSpeed;
        return desiredVelocity - boid.Velocity;
    }
    private Vector2 CalculateAlignmentSteering(
        Boid boid,
        Vector2 alignmentVelocitySum,
        int neighborCount)
    {
        if (neighborCount == 0)
        {
            return Vector2.Zero;
        }
        var count = (float)neighborCount;
        if (alignmentVelocitySum.LengthSquared() <=
            Epsilon * count * count)
        {
            return Vector2.Zero - boid.Velocity;
        }
        var desiredVelocity =
            Vector2.Normalize(alignmentVelocitySum) *
            _settings.MaxSpeed;
        return desiredVelocity - boid.Velocity;
    }
    private Vector2 CalculateCohesionSteering(
        Boid boid,
        Vector2 cohesionOffsetSum,
        int neighborCount)
    {
        if (neighborCount == 0)
        {
            return Vector2.Zero;
        }
        var count = (float)neighborCount;
        if (cohesionOffsetSum.LengthSquared() <=
            Epsilon * count * count)
        {
            return Vector2.Zero;
        }
        var desiredVelocity =
            Vector2.Normalize(cohesionOffsetSum) *
            _settings.MaxSpeed;
        return desiredVelocity - boid.Velocity;
    }
    private Vector2 GetWrappedOffset(
        Vector2 from,
        Vector2 to)
    {
        var offset = to - from;
        var halfWidth = _settings.WorldWidth / 2f;
        var halfHeight = _settings.WorldHeight / 2f;
        if (offset.X > halfWidth)
        {
            offset.X -= _settings.WorldWidth;
        }
        else if (offset.X < -halfWidth)
        {
            offset.X += _settings.WorldWidth;
        }
        if (offset.Y > halfHeight)
        {
            offset.Y -= _settings.WorldHeight;
        }
        else if (offset.Y < -halfHeight)
        {
            offset.Y += _settings.WorldHeight;
        }
        return offset;
    }
    private void EnsureCapacity(int populationCount)
    {
        if (_populationBuffer.Length != populationCount)
        {
            Array.Resize(
                ref _populationBuffer,
                populationCount);
        }
        if (_nextVelocities.Length != populationCount)
        {
            Array.Resize(
                ref _nextVelocities,
                populationCount);
        }
        if (_evaluationsPerBoid.Length != populationCount)
        {
            Array.Resize(
                ref _evaluationsPerBoid,
                populationCount);
        }
    }
    private static Vector2 ClampMagnitude(
        Vector2 vector,
        float maxMagnitude)
    {
        if (maxMagnitude <= 0f)
        {
            return Vector2.Zero;
        }
        var lengthSquared = vector.LengthSquared();
        var maxMagnitudeSquared = maxMagnitude * maxMagnitude;
        if (lengthSquared <= maxMagnitudeSquared ||
            lengthSquared <= Epsilon)
        {
            return vector;
        }
        var scale = maxMagnitude / MathF.Sqrt(lengthSquared);
        return vector * scale;
    }
    private static float WrapCoordinate(
        float coordinate,
        float worldSize,
        float halfWorldSize)
    {
        var wrapped = coordinate % worldSize;
        if (wrapped < -halfWorldSize)
        {
            wrapped += worldSize;
        }
        else if (wrapped >= halfWorldSize)
        {
            wrapped -= worldSize;
        }
        return wrapped;
    }
}

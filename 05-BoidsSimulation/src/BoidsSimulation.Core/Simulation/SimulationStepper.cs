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
        0,
        0L,
        0L,
        0L,
        0L,
        0L,
        0L,
        0,
        0,
        0,
        0.0,
        0,
        0,
        0);
}

public sealed class SimulationStepper
{
    private const float Epsilon = 1e-6f;

    private readonly SimulationSettings _settings;
    private readonly BoidSpatialGrid _grid;
    private readonly int[] _candidateBuffer;

    private readonly float _separationRadiusSquared;
    private readonly float _alignmentRadiusSquared;
    private readonly float _cohesionRadiusSquared;
    private readonly float _searchRadius;
    private readonly float _searchRadiusSquared;

    private readonly int[] _evaluationHistogram;

    private Boid[] _populationBuffer = Array.Empty<Boid>();
    private Vector2[] _nextVelocities = Array.Empty<Vector2>();

    private struct SteeringWorkAccumulator
    {
        public long GridCandidatesReturned;
        public long NeighborCandidatesExamined;
        public long SquaredDistanceCalculations;

        public long SeparationMatches;
        public long AlignmentMatches;
        public long CohesionMatches;

        public int BoidsWithZeroEvaluatedNeighbors;
        public int BoidsAtEvaluationLimit;
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

        _settings = settings;

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

        _candidateBuffer =
            new int[settings.MaxNeighborEvaluationsPerBoid + 1];

        _evaluationHistogram =
            new int[settings.MaxNeighborEvaluationsPerBoid + 1];

        _grid = new BoidSpatialGrid(
            settings.WorldWidth,
            settings.WorldHeight,
            settings.CellSize,
            Array.Empty<Boid>());
    }

    public void Step(SimulationState state)
    {
        ArgumentNullException.ThrowIfNull(state);

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
        phaseStart = Stopwatch.GetTimestamp();

        var work = new SteeringWorkAccumulator();

        var minimumEvaluations = int.MaxValue;
        var maximumEvaluations = 0;

        for (var i = 0; i < populationCount; i++)
        {
            var boid = _populationBuffer[i];

            var steering = CalculateSteering(
                i,
                boid,
                ref work,
                out var evaluationsForBoid);

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

            if (evaluationsForBoid >=
                _settings.MaxNeighborEvaluationsPerBoid)
            {
                work.BoidsAtEvaluationLimit++;
            }

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

    private Vector2 CalculateSteering(
        int boidIndex,
        Boid boid,
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
            _candidateBuffer.AsSpan());

        work.GridCandidatesReturned += candidateCount;

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
            var candidateIndex = _candidateBuffer[i];

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

    private SimulationStepWorkMetrics CreateWorkMetrics(
        int populationCount,
        SteeringWorkAccumulator work,
        int minimumEvaluations,
        int maximumEvaluations)
    {
        return new SimulationStepWorkMetrics(
            BoidCount: populationCount,
            GridCandidatesReturned: work.GridCandidatesReturned,
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
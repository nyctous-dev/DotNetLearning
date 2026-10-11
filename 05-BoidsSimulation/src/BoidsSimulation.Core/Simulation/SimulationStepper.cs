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

    private Boid[] _populationBuffer = Array.Empty<Boid>();
    private Vector2[] _nextVelocities = Array.Empty<Vector2>();

    public SimulationStepTimings LastStepTimings { get; private set; } =
        SimulationStepTimings.Empty;

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

        // These settings do not change during this stepper's lifetime,
        // so calculate the broad-phase search radius once.
        _searchRadius = MathF.Max(
            settings.SeparationRadius,
            MathF.Max(
                settings.AlignmentRadius,
                settings.CohesionRadius));
        
        _searchRadiusSquared = _searchRadius * _searchRadius;

        _candidateBuffer =
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
            return;
        }

        // Phase 1: Prepare reusable buffers and population ordering.
        var phaseStart = Stopwatch.GetTimestamp();

        EnsureCapacity(populationCount);

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

        for (var i = 0; i < populationCount; i++)
        {
            var boid = _populationBuffer[i];
            var steering = CalculateSteering(i, boid);

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
        Boid boid)
    {
        if (_searchRadius <= 0f)
        {
            return Vector2.Zero;
        }

        var candidateCount = _grid.QueryNeighborhoodIndices(
            boid.Position,
            _searchRadius,
            _candidateBuffer.AsSpan());

        var separationSum = Vector2.Zero;
        var separationCount = 0;

        var alignmentVelocitySum = Vector2.Zero;
        var alignmentCount = 0;

        var cohesionOffsetSum = Vector2.Zero;
        var cohesionCount = 0;

        var neighborEvaluations = 0;
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

            var neighbor = _populationBuffer[candidateIndex];

            var offset = GetWrappedOffset(
                boid.Position,
                neighbor.Position);

            // Reject candidates outside the largest possible influence radius
            // before calculating squared distance.
            if (MathF.Abs(offset.X) > _searchRadius ||
                MathF.Abs(offset.Y) > _searchRadius)
            {
                continue;
            }

            var distanceSquared = offset.LengthSquared();

            // The candidate is inside the bounding square but outside the
            // circular search radius, so none of the steering rules can use it.
            if (distanceSquared > _searchRadiusSquared)
            {
                continue;
            }

            if (_separationRadiusSquared > 0f &&
                distanceSquared > Epsilon &&
                distanceSquared <= _separationRadiusSquared)
            {
                // Give closer boids greater influence on separation.
                separationSum -= offset / distanceSquared;
                separationCount++;
            }

            if (_alignmentRadiusSquared > 0f &&
                distanceSquared <= _alignmentRadiusSquared)
            {
                alignmentVelocitySum += neighbor.Velocity;
                alignmentCount++;
            }

            if (_cohesionRadiusSquared > 0f &&
                distanceSquared <= _cohesionRadiusSquared)
            {
                cohesionOffsetSum += offset;
                cohesionCount++;
            }

            if (_separationRadiusSquared > 0f &&
                distanceSquared > Epsilon &&
                distanceSquared <= _separationRadiusSquared)
            {
                // Give closer boids greater influence on separation.
                separationSum -= offset / distanceSquared;
                separationCount++;
            }

            if (_alignmentRadiusSquared > 0f &&
                distanceSquared <= _alignmentRadiusSquared)
            {
                alignmentVelocitySum += neighbor.Velocity;
                alignmentCount++;
            }

            if (_cohesionRadiusSquared > 0f &&
                distanceSquared <= _cohesionRadiusSquared)
            {
                cohesionOffsetSum += offset;
                cohesionCount++;
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

    private Vector2 CalculateSeparationSteering(
        Boid boid,
        Vector2 separationSum,
        int neighborCount)
    {
        if (neighborCount == 0)
        {
            return Vector2.Zero;
        }

        // The old code tested (separationSum / neighborCount).LengthSquared()
        // against Epsilon. Apply the equivalent threshold to the sum to
        // avoid dividing both vector components by the neighbor count.
        var count = (float)neighborCount;

        if (separationSum.LengthSquared() <=
            Epsilon * count * count)
        {
            return Vector2.Zero;
        }

        // Dividing by a positive count changes magnitude, not direction.
        // Normalize the sum directly to avoid an intermediate vector divide.
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

        // Preserve the previous near-zero test on the average velocity.
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

        // Preserve the previous near-zero test on the average offset.
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
        // Keep the buffers exactly sized to the population, so the grid
        // can rebuild from the same array without an ArraySegment wrapper.
        // Buffers are reused while the population size remains stable.
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
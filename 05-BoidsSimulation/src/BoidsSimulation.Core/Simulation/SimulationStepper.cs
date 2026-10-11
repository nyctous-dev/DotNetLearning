
using System.Numerics;
using BoidsSimulation.Core.Models;

namespace BoidsSimulation.Core.Simulation;

public sealed class SimulationStepper
{
    private readonly SimulationSettings _settings;

    public SimulationStepper(
        SimulationSettings settings,
        SimulationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(limits);

        settings.Validate(limits);

        _settings = settings;
    }

    public void Step(SimulationState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var population = state.Boids.ToArray();

        if (population.Length == 0)
        {
            return;
        }

        // Capture the state at the beginning of the tick.
        var snapshot = population
            .Select(boid => new Boid(
                boid.Id,
                boid.Position,
                boid.Velocity))
            .ToArray();

        var boidsById = snapshot.ToDictionary(boid => boid.Id);

        var grid = new BoidSpatialGrid(
            _settings.WorldWidth,
            _settings.WorldHeight,
            _settings.CellSize,
            snapshot);

        var nextVelocities = new Vector2[population.Length];

        for (var i = 0; i < snapshot.Length; i++)
        {
            var boid = snapshot[i];

            var steering = CalculateSteering(
                boid,
                grid,
                boidsById);

            if (steering == Vector2.Zero)
            {
                nextVelocities[i] = boid.Velocity;
                continue;
            }

            steering = ClampMagnitude(
                steering,
                _settings.MaxSteeringForce);

            var nextVelocity =
                boid.Velocity + steering * _settings.FixedTimeStep;

            nextVelocities[i] = ClampMagnitude(
                nextVelocity,
                _settings.MaxSpeed);
        }

        // Commit velocities only after all steering calculations finish.
        for (var i = 0; i < population.Length; i++)
        {
            population[i].Velocity = nextVelocities[i];
        }

        var worldWidth = (float)_settings.WorldWidth;
        var worldHeight = (float)_settings.WorldHeight;
        var halfWidth = worldWidth / 2f;
        var halfHeight = worldHeight / 2f;
        var timeStep = _settings.FixedTimeStep;

        foreach (var boid in population)
        {
            var nextPosition =
                boid.Position + boid.Velocity * timeStep;

            boid.Position = new Vector2(
                WrapCoordinate(nextPosition.X, worldWidth, halfWidth),
                WrapCoordinate(nextPosition.Y, worldHeight, halfHeight));
        }
    }

    private Vector2 CalculateSteering(
        Boid boid,
        BoidSpatialGrid grid,
        IReadOnlyDictionary<int, Boid> boidsById)
    {
        var searchRadius = MathF.Max(
            _settings.SeparationRadius,
            MathF.Max(
                _settings.AlignmentRadius,
                _settings.CohesionRadius));

        if (searchRadius <= 0f)
        {
            return Vector2.Zero;
        }

        // One spatial query and one shared candidate-evaluation budget
        // for all three behaviors.
        var candidateLimit =
            _settings.MaxNeighborEvaluationsPerBoid + 1;

        var candidateIds = grid.QueryNeighborhood(
            boid.Position,
            searchRadius,
            candidateLimit);

        var separationDirection = Vector2.Zero;
        var alignmentVelocitySum = Vector2.Zero;
        var alignmentNeighborCount = 0;
        var cohesionOffsetSum = Vector2.Zero;
        var cohesionNeighborCount = 0;

        var evaluatedNeighbors = 0;

        var separationRadiusSquared =
            _settings.SeparationRadius * _settings.SeparationRadius;

        var alignmentRadiusSquared =
            _settings.AlignmentRadius * _settings.AlignmentRadius;

        var cohesionRadiusSquared =
            _settings.CohesionRadius * _settings.CohesionRadius;

        foreach (var candidateId in candidateIds)
        {
            if (candidateId == boid.Id)
            {
                continue;
            }

            if (evaluatedNeighbors >=
                _settings.MaxNeighborEvaluationsPerBoid)
            {
                break;
            }

            evaluatedNeighbors++;

            var neighbor = boidsById[candidateId];

            var offset = GetWrappedOffset(
                boid.Position,
                neighbor.Position);

            var distanceSquared = offset.LengthSquared();

            if (_settings.SeparationRadius > 0f &&
                distanceSquared > 0f &&
                distanceSquared <= separationRadiusSquared)
            {
                // Closer neighbors contribute more strongly.
                separationDirection -= offset / distanceSquared;
            }

            if (_settings.AlignmentRadius > 0f &&
                distanceSquared <= alignmentRadiusSquared)
            {
                alignmentVelocitySum += neighbor.Velocity;
                alignmentNeighborCount++;
            }

            if (_settings.CohesionRadius > 0f &&
                distanceSquared <= cohesionRadiusSquared)
            {
                // Offset is relative to this boid and uses the shortest
                // wrapped displacement across world boundaries.
                cohesionOffsetSum += offset;
                cohesionNeighborCount++;
            }
        }

        var separationSteering = CalculateSeparationSteering(
            boid,
            separationDirection);

        var alignmentSteering = CalculateAlignmentSteering(
            boid,
            alignmentVelocitySum,
            alignmentNeighborCount);

        var cohesionSteering = CalculateCohesionSteering(
            boid,
            cohesionOffsetSum,
            cohesionNeighborCount);

        return
            separationSteering * _settings.SeparationWeight +
            alignmentSteering * _settings.AlignmentWeight +
            cohesionSteering * _settings.CohesionWeight;
    }

    private Vector2 CalculateSeparationSteering(
        Boid boid,
        Vector2 separationDirection)
    {
        if (separationDirection.LengthSquared() <= 0f)
        {
            return Vector2.Zero;
        }

        var desiredVelocity =
            Vector2.Normalize(separationDirection) * _settings.MaxSpeed;

        return desiredVelocity - boid.Velocity;
    }

    private Vector2 CalculateAlignmentSteering(
        Boid boid,
        Vector2 alignmentVelocitySum,
        int alignmentNeighborCount)
    {
        if (alignmentNeighborCount == 0)
        {
            return Vector2.Zero;
        }

        var averageVelocity =
            alignmentVelocitySum / alignmentNeighborCount;

        if (averageVelocity.LengthSquared() <= 0f)
        {
            return Vector2.Zero;
        }

        var desiredVelocity =
            Vector2.Normalize(averageVelocity) * _settings.MaxSpeed;

        return desiredVelocity - boid.Velocity;
    }

    private Vector2 CalculateCohesionSteering(
        Boid boid,
        Vector2 cohesionOffsetSum,
        int cohesionNeighborCount)
    {
        if (cohesionNeighborCount == 0)
        {
            return Vector2.Zero;
        }

        var averageOffset =
            cohesionOffsetSum / cohesionNeighborCount;

        if (averageOffset.LengthSquared() <= 0f)
        {
            return Vector2.Zero;
        }

        // Seek toward the average position of eligible neighbors.
        var desiredVelocity =
            Vector2.Normalize(averageOffset) * _settings.MaxSpeed;

        return desiredVelocity - boid.Velocity;
    }

    private Vector2 GetWrappedOffset(
        Vector2 position,
        Vector2 otherPosition)
    {
        var offset = otherPosition - position;

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

    private static Vector2 ClampMagnitude(
        Vector2 vector,
        float maxMagnitude)
    {
        var lengthSquared = vector.LengthSquared();
        var maxMagnitudeSquared = maxMagnitude * maxMagnitude;

        if (lengthSquared <= maxMagnitudeSquared || lengthSquared <= 0f)
        {
            return vector;
        }

        return Vector2.Normalize(vector) * maxMagnitude;
    }

    private static float WrapCoordinate(
        float coordinate,
        float worldSize,
        float halfWorldSize)
    {
        var wrapped = (coordinate + halfWorldSize) % worldSize;

        if (wrapped < 0f)
        {
            wrapped += worldSize;
        }

        return wrapped - halfWorldSize;
    }
}

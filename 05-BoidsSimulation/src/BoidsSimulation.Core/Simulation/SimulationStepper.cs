
using System.Numerics;
using BoidsSimulation.Core.Models;

namespace BoidsSimulation.Core.Simulation;

public sealed class SimulationStepper
{
    private readonly SimulationSettings _settings;
    private readonly BoidSpatialGrid _grid;
    private readonly Dictionary<int, Boid> _boidsById = new();
    private readonly int[] _candidateBuffer;

    private Boid[] _populationBuffer = Array.Empty<Boid>();
    private Vector2[] _nextVelocities = Array.Empty<Vector2>();

    public SimulationStepper(
        SimulationSettings settings,
        SimulationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(limits);

        settings.Validate(limits);

        _settings = settings;

        _grid = new BoidSpatialGrid(
            settings.WorldWidth,
            settings.WorldHeight,
            settings.CellSize,
            Array.Empty<Boid>());

        _candidateBuffer = new int[
            settings.MaxNeighborEvaluationsPerBoid + 1];
    }

    public void Step(SimulationState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var populationCount = state.BoidCount;

        if (populationCount == 0)
        {
            return;
        }

        EnsureCapacity(populationCount);

        var index = 0;

        foreach (var boid in state.Boids)
        {
            _populationBuffer[index++] = boid;
        }

        var population = new ArraySegment<Boid>(
            _populationBuffer,
            0,
            populationCount);

        // Rebuild the reusable grid once per tick. No boid positions or
        // velocities are changed until all steering calculations finish.
        _grid.Rebuild(population);

        _boidsById.Clear();

        for (var i = 0; i < populationCount; i++)
        {
            var boid = _populationBuffer[i];
            _boidsById.Add(boid.Id, boid);
        }

        // Calculate every new velocity from the same start-of-tick state.
        for (var i = 0; i < populationCount; i++)
        {
            var boid = _populationBuffer[i];

            var steering = CalculateSteering(boid);

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

        // Commit velocities only after every steering calculation finishes.
        for (var i = 0; i < populationCount; i++)
        {
            _populationBuffer[i].Velocity = _nextVelocities[i];
        }

        var worldWidth = (float)_settings.WorldWidth;
        var worldHeight = (float)_settings.WorldHeight;
        var halfWidth = worldWidth / 2f;
        var halfHeight = worldHeight / 2f;
        var timeStep = _settings.FixedTimeStep;

        // Move all boids using their newly calculated velocities.
        for (var i = 0; i < populationCount; i++)
        {
            var boid = _populationBuffer[i];

            var nextPosition =
                boid.Position + boid.Velocity * timeStep;

            boid.Position = new Vector2(
                WrapCoordinate(nextPosition.X, worldWidth, halfWidth),
                WrapCoordinate(nextPosition.Y, worldHeight, halfHeight));
        }
    }

    private Vector2 CalculateSteering(Boid boid)
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

        var candidateCount = _grid.QueryNeighborhood(
            boid.Position,
            searchRadius,
            _candidateBuffer.AsSpan());

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

        for (var i = 0; i < candidateCount; i++)
        {
            var candidateId = _candidateBuffer[i];

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

            var neighbor = _boidsById[candidateId];

            var offset = GetWrappedOffset(
                boid.Position,
                neighbor.Position);

            var distanceSquared = offset.LengthSquared();

            if (_settings.SeparationRadius > 0f &&
                distanceSquared > 0f &&
                distanceSquared <= separationRadiusSquared)
            {
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

    private void EnsureCapacity(int requiredCapacity)
    {
        if (_populationBuffer.Length >= requiredCapacity)
        {
            return;
        }

        var newCapacity = _populationBuffer.Length == 0
            ? requiredCapacity
            : Math.Max(
                requiredCapacity,
                _populationBuffer.Length * 2);

        Array.Resize(ref _populationBuffer, newCapacity);
        Array.Resize(ref _nextVelocities, newCapacity);
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

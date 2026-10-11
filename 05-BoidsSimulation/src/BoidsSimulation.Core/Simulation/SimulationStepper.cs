
using BoidsSimulation.Core.Models;

namespace BoidsSimulation.Core.Simulation;

public sealed class SimulationStepper
{
    private readonly SimulationSettings _settings;
    private readonly SimulationLimits _limits;

    public SimulationStepper(
        SimulationSettings settings,
        SimulationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(limits);

        settings.Validate(limits);

        _settings = settings;
        _limits = limits;
    }

    public void Step(SimulationState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var timeStep = _settings.FixedTimeStep;
        var worldWidth = (float)_settings.WorldWidth;
        var worldHeight = (float)_settings.WorldHeight;

        var halfWidth = worldWidth / 2f;
        var halfHeight = worldHeight / 2f;

        foreach (var boid in state.Boids)
        {
            var nextPosition =
                boid.Position + boid.Velocity * timeStep;

            boid.Position = new System.Numerics.Vector2(
                WrapCoordinate(nextPosition.X, worldWidth, halfWidth),
                WrapCoordinate(nextPosition.Y, worldHeight, halfHeight));
        }
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

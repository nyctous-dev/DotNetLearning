
using System.Numerics;
using BoidsSimulation.Core.Models;

namespace BoidsSimulation.Core.Simulation;

public sealed class BoidPopulationInitializer
{
    private readonly SimulationSettings _settings;
    private readonly SimulationLimits _limits;

    public BoidPopulationInitializer(
        SimulationSettings settings,
        SimulationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(limits);

        _settings = settings;
        _limits = limits;
    }

    public IReadOnlyList<Boid> CreateInitialPopulation(int seed)
    {
        _settings.Validate(_limits);

        var random = new Random(seed);
        var population = new List<Boid>(_settings.InitialBoidCount);

        var halfWidth = _settings.WorldWidth / 2f;
        var halfHeight = _settings.WorldHeight / 2f;

        for (var i = 0; i < _settings.InitialBoidCount; i++)
        {
            var id = i + 1;

            var position = CreateRandomPosition(
                random,
                halfWidth,
                halfHeight);

            var velocity = CreateRandomVelocity(random);

            population.Add(new Boid(id, position, velocity));
        }

        return population;
    }

    private static Vector2 CreateRandomPosition(
        Random random,
        float halfWidth,
        float halfHeight)
    {
        var x = random.NextSingle() * (halfWidth * 2f) - halfWidth;
        var y = random.NextSingle() * (halfHeight * 2f) - halfHeight;

        return new Vector2(x, y);
    }

    private Vector2 CreateRandomVelocity(Random random)
    {
        var angle = random.NextSingle() * (2f * MathF.PI);

        // Keep initial speeds below MaxSpeed to avoid exceeding it
        // because of floating-point rounding.
        var speed = _settings.MaxSpeed *
                    (0.1f + random.NextSingle() * 0.8f);

        return new Vector2(
            MathF.Cos(angle) * speed,
            MathF.Sin(angle) * speed);
    }
}

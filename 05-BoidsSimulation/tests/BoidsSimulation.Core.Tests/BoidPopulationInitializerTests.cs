
using System;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using BoidsSimulation.Core.Models;
using BoidsSimulation.Core.Simulation;

namespace BoidsSimulation.Core.Tests;

public sealed class BoidPopulationInitializerTests
{
    private sealed record TestConfiguration(
        SimulationSettings Settings,
        SimulationLimits Limits);

    private static TestConfiguration LoadConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(
                "appsettings.json",
                optional: false,
                reloadOnChange: false)
            .Build();

        var settings = configuration
            .GetSection("Simulation")
            .Get<SimulationSettings>()
            ?? throw new InvalidOperationException(
                "Missing Simulation configuration.");

        var limits = configuration
            .GetSection("SimulationLimits")
            .Get<SimulationLimits>()
            ?? throw new InvalidOperationException(
                "Missing SimulationLimits configuration.");

        return new TestConfiguration(settings, limits);
    }

    private static BoidPopulationInitializer CreateInitializer(
        TestConfiguration configuration)
    {
        return new BoidPopulationInitializer(
            configuration.Settings,
            configuration.Limits);
    }

    [Fact]
    public void CreateInitialPopulation_ReturnsConfiguredNumberOfBoids()
    {
        var configuration = LoadConfiguration();
        var initializer = CreateInitializer(configuration);

        var population = initializer.CreateInitialPopulation(seed: 12345);

        Assert.Equal(
            configuration.Settings.InitialBoidCount,
            population.Count);
    }

    [Fact]
    public void CreateInitialPopulation_UsesCenteredWorldCoordinates()
    {
        var configuration = LoadConfiguration();
        var initializer = CreateInitializer(configuration);

        var population = initializer.CreateInitialPopulation(seed: 12345);

        var halfWidth = configuration.Settings.WorldWidth / 2f;
        var halfHeight = configuration.Settings.WorldHeight / 2f;
        var minimumX = -halfWidth;
        var minimumY = -halfHeight;

        foreach (var boid in population)
        {
            Assert.True(
                boid.Position.X >= minimumX,
                $"Boid X position {boid.Position.X} is below {minimumX}.");

            Assert.True(
                boid.Position.X < halfWidth,
                $"Boid X position {boid.Position.X} is at or beyond {halfWidth}.");

            Assert.True(
                boid.Position.Y >= minimumY,
                $"Boid Y position {boid.Position.Y} is below {minimumY}.");

            Assert.True(
                boid.Position.Y < halfHeight,
                $"Boid Y position {boid.Position.Y} is at or beyond {halfHeight}.");
        }
    }

    [Fact]
    public void CreateInitialPopulation_GivesEveryBoidAValidVelocity()
    {
        var configuration = LoadConfiguration();
        var initializer = CreateInitializer(configuration);

        var population = initializer.CreateInitialPopulation(seed: 12345);

        foreach (var boid in population)
        {
            var speed = boid.Velocity.Length();

            Assert.True(
                float.IsFinite(speed),
                $"Velocity magnitude must be finite, but was {speed}.");

            Assert.True(
                speed > 0f,
                "Every boid must start with a nonzero velocity.");

            Assert.True(
                speed <= configuration.Settings.MaxSpeed,
                $"Speed {speed} exceeds MaxSpeed {configuration.Settings.MaxSpeed}.");
        }
    }

    [Fact]
    public void CreateInitialPopulation_SameSeedProducesSamePopulation()
    {
        var configuration = LoadConfiguration();
        var initializer = CreateInitializer(configuration);
        const int seed = 12345;

        var firstPopulation =
            initializer.CreateInitialPopulation(seed);

        var secondPopulation =
            initializer.CreateInitialPopulation(seed);

        Assert.Equal(firstPopulation.Count, secondPopulation.Count);

        for (var index = 0; index < firstPopulation.Count; index++)
        {
            Assert.Equal(
                firstPopulation[index].Position,
                secondPopulation[index].Position);

            Assert.Equal(
                firstPopulation[index].Velocity,
                secondPopulation[index].Velocity);
        }
    }

    [Fact]
    public void CreateInitialPopulation_ZeroBoidsReturnsEmptyPopulation()
    {
        var configuration = LoadConfiguration();

        var settings = configuration.Settings with
        {
            InitialBoidCount = 0
        };

        var initializer = new BoidPopulationInitializer(
            settings,
            configuration.Limits);

        var population = initializer.CreateInitialPopulation(seed: 12345);

        Assert.Empty(population);
    }

    [Fact]
    public void CreateInitialPopulation_RejectsInvalidSettings()
    {
        var configuration = LoadConfiguration();

        var invalidSettings = configuration.Settings with
        {
            InitialBoidCount = configuration.Settings.MaxBoids + 1
        };

        var initializer = new BoidPopulationInitializer(
            invalidSettings,
            configuration.Limits);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            initializer.CreateInitialPopulation(seed: 12345);
        });
    }

    [Fact]
    public void CreateInitialPopulation_HandlesOddWorldDimensions()
    {
        var configuration = LoadConfiguration();

        var settings = configuration.Settings with
        {
            WorldWidth = 10001,
            WorldHeight = 9999,
            InitialBoidCount = 100
        };

        var initializer = new BoidPopulationInitializer(
            settings,
            configuration.Limits);

        var population = initializer.CreateInitialPopulation(seed: 12345);

        var halfWidth = settings.WorldWidth / 2f;
        var halfHeight = settings.WorldHeight / 2f;

        foreach (var boid in population)
        {
            Assert.InRange(
                boid.Position.X,
                -halfWidth,
                MathF.BitDecrement(halfWidth));

            Assert.InRange(
                boid.Position.Y,
                -halfHeight,
                MathF.BitDecrement(halfHeight));
        }
    }

    [Fact]
    public void CreateInitialPopulation_WorksAtMinimumAllowedMaxSpeed()
    {
        var configuration = LoadConfiguration();

        var (minimumSpeed, _) =
            configuration.Limits.MaxSpeed.GetBounds(
                nameof(SimulationSettings.MaxSpeed));

        var settings = configuration.Settings with
        {
            MaxSpeed = minimumSpeed,
            InitialBoidCount = 100
        };

        var initializer = new BoidPopulationInitializer(
            settings,
            configuration.Limits);

        var population = initializer.CreateInitialPopulation(seed: 12345);

        foreach (var boid in population)
        {
            var speed = boid.Velocity.Length();

            Assert.True(float.IsFinite(speed));
            Assert.True(speed > 0f);
            Assert.True(speed <= settings.MaxSpeed);
        }
    }

    [Fact]
    public void CreateInitialPopulation_AllowsInitialCountEqualToMaxBoids()
    {
        var configuration = LoadConfiguration();

        var (minimumBoidLimit, _) =
            configuration.Limits.MaxBoids.GetBounds(
                nameof(SimulationSettings.MaxBoids));

        var settings = configuration.Settings with
        {
            MaxBoids = minimumBoidLimit,
            InitialBoidCount = minimumBoidLimit
        };

        var initializer = new BoidPopulationInitializer(
            settings,
            configuration.Limits);

        var population = initializer.CreateInitialPopulation(seed: 12345);

        Assert.Equal(settings.MaxBoids, population.Count);
    }

}

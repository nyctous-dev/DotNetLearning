using System.Numerics;
using BoidsSimulation.Core.Models;
using BoidsSimulation.Core.Simulation;

namespace BoidsSimulation.Core.Tests;

public sealed class BoidPopulationInitializerTests
{
    private static readonly SimulationSettings DefaultSettings = new()
    {
        WorldWidth = 100,
        WorldHeight = 80,
        MaxBoids = 100,
        InitialBoidCount = 25,

        SeparationRadius = 10f,
        AlignmentRadius = 20f,
        CohesionRadius = 30f,
        CellSize = 10f,

        SeparationWeight = 1.5f,
        AlignmentWeight = 1f,
        CohesionWeight = 1f,

        MaxSpeed = 10f,
        MaxSteeringForce = 5f,
        AttractionForce = 5f,
        RepulsionForce = 5f,
        InteractionRadius = 10f,
        FixedTimeStep = 0.016666667f,

        MaxNeighborEvaluationsPerBoid = 50
    };

    private static readonly SimulationLimits Limits = new()
    {
        WorldWidth = new SimulationLimits.IntegerRange
        {
            Min = 1,
            Max = 100_000
        },
        WorldHeight = new SimulationLimits.IntegerRange
        {
            Min = 1,
            Max = 100_000
        },
        MaxBoids = new SimulationLimits.IntegerRange
        {
            Min = 1,
            Max = 100_000
        },
        InitialBoidCount = new SimulationLimits.IntegerRange
        {
            Min = 0,
            Max = 100_000
        },

        SeparationRadius = new SimulationLimits.FloatRange
        {
            Min = 0f,
            Max = 10_000f
        },
        AlignmentRadius = new SimulationLimits.FloatRange
        {
            Min = 0f,
            Max = 10_000f
        },
        CohesionRadius = new SimulationLimits.FloatRange
        {
            Min = 0f,
            Max = 10_000f
        },
        CellSize = new SimulationLimits.FloatRange
        {
            Min = 1f,
            Max = 10_000f
        },

        SeparationWeight = new SimulationLimits.FloatRange
        {
            Min = 0f,
            Max = 100f
        },
        AlignmentWeight = new SimulationLimits.FloatRange
        {
            Min = 0f,
            Max = 100f
        },
        CohesionWeight = new SimulationLimits.FloatRange
        {
            Min = 0f,
            Max = 100f
        },

        MaxSpeed = new SimulationLimits.FloatRange
        {
            Min = 0.001f,
            Max = 10_000f
        },
        MaxSteeringForce = new SimulationLimits.FloatRange
        {
            Min = 0f,
            Max = 10_000f
        },
        AttractionForce = new SimulationLimits.FloatRange
        {
            Min = 0f,
            Max = 10_000f
        },
        RepulsionForce = new SimulationLimits.FloatRange
        {
            Min = 0f,
            Max = 10_000f
        },
        InteractionRadius = new SimulationLimits.FloatRange
        {
            Min = 0f,
            Max = 10_000f
        },
        FixedTimeStep = new SimulationLimits.FloatRange
        {
            Min = 0.001f,
            Max = 0.1f
        },

        MaxNeighborEvaluationsPerBoid =
            new SimulationLimits.IntegerRange
            {
                Min = 1,
                Max = 10_000
            }
    };

    [Fact]
    public void CreateInitialPopulation_CreatesConfiguredNumberOfBoids()
    {
        var settings = DefaultSettings with
        {
            InitialBoidCount = 25
        };

        var initializer = new BoidPopulationInitializer(settings, Limits);

        var population = initializer.CreateInitialPopulation(seed: 123);

        Assert.Equal(settings.InitialBoidCount, population.Count);
    }

    [Fact]
    public void CreateInitialPopulation_AssignsSequentialPositiveIds()
    {
        var settings = DefaultSettings with
        {
            InitialBoidCount = 10
        };

        var initializer = new BoidPopulationInitializer(settings, Limits);

        var population = initializer.CreateInitialPopulation(seed: 123);

        Assert.Equal(
            Enumerable.Range(1, settings.InitialBoidCount),
            population.Select(boid => boid.Id));

        Assert.All(population, boid => Assert.True(boid.Id > 0));
    }

    [Fact]
    public void CreateInitialPopulation_KeepsPositionsInsideCenteredWorld()
    {
        var settings = DefaultSettings with
        {
            InitialBoidCount = 100
        };

        var initializer = new BoidPopulationInitializer(settings, Limits);

        var population = initializer.CreateInitialPopulation(seed: 123);

        var halfWidth = settings.WorldWidth / 2f;
        var halfHeight = settings.WorldHeight / 2f;

        Assert.All(population, boid =>
        {
            Assert.True(
                boid.Position.X >= -halfWidth &&
                boid.Position.X < halfWidth,
                $"X coordinate {boid.Position.X} is outside the world.");

            Assert.True(
                boid.Position.Y >= -halfHeight &&
                boid.Position.Y < halfHeight,
                $"Y coordinate {boid.Position.Y} is outside the world.");
        });
    }

    [Fact]
    public void CreateInitialPopulation_GeneratesValidVelocities()
    {
        var settings = DefaultSettings with
        {
            InitialBoidCount = 100
        };

        var initializer = new BoidPopulationInitializer(settings, Limits);

        var population = initializer.CreateInitialPopulation(seed: 123);

        Assert.All(population, boid =>
        {
            var speed = boid.Velocity.Length();

            Assert.True(
                speed > 0f,
                $"Boid {boid.Id} has zero speed.");

            Assert.True(
                speed <= settings.MaxSpeed,
                $"Boid {boid.Id} exceeds the configured maximum speed.");
        });
    }

    [Fact]
    public void CreateInitialPopulation_ProducesSameResultsForSameSeed()
    {
        var settings = DefaultSettings with
        {
            InitialBoidCount = 25
        };

        var initializer = new BoidPopulationInitializer(settings, Limits);

        var firstPopulation = initializer.CreateInitialPopulation(seed: 42);
        var secondPopulation = initializer.CreateInitialPopulation(seed: 42);

        Assert.Equal(firstPopulation.Count, secondPopulation.Count);

        for (var i = 0; i < firstPopulation.Count; i++)
        {
            Assert.Equal(firstPopulation[i].Id, secondPopulation[i].Id);
            Assert.Equal(firstPopulation[i].Position, secondPopulation[i].Position);
            Assert.Equal(firstPopulation[i].Velocity, secondPopulation[i].Velocity);
        }
    }

    [Fact]
    public void CreateInitialPopulation_ProducesDifferentResultsForDifferentSeeds()
    {
        var settings = DefaultSettings with
        {
            InitialBoidCount = 25
        };

        var initializer = new BoidPopulationInitializer(settings, Limits);

        var firstPopulation = initializer.CreateInitialPopulation(seed: 42);
        var secondPopulation = initializer.CreateInitialPopulation(seed: 43);

        Assert.NotEqual(
            firstPopulation[0].Position,
            secondPopulation[0].Position);
    }

    [Fact]
    public void CreateInitialPopulation_AllowsZeroBoids()
    {
        var settings = DefaultSettings with
        {
            InitialBoidCount = 0
        };

        var initializer = new BoidPopulationInitializer(settings, Limits);

        var population = initializer.CreateInitialPopulation(seed: 123);

        Assert.Empty(population);
    }

    [Fact]
    public void CreateInitialPopulation_HandlesOddWorldDimensions()
    {
        var settings = DefaultSettings with
        {
            WorldWidth = 101,
            WorldHeight = 99,
            InitialBoidCount = 100
        };

        var initializer = new BoidPopulationInitializer(settings, Limits);

        var population = initializer.CreateInitialPopulation(seed: 123);

        var halfWidth = settings.WorldWidth / 2f;
        var halfHeight = settings.WorldHeight / 2f;

        Assert.All(population, boid =>
        {
            Assert.True(boid.Position.X >= -halfWidth);
            Assert.True(boid.Position.X < halfWidth);

            Assert.True(boid.Position.Y >= -halfHeight);
            Assert.True(boid.Position.Y < halfHeight);
        });
    }

    [Fact]
    public void CreateInitialPopulation_HandlesMinimumAllowedMaxSpeed()
    {
        var settings = DefaultSettings with
        {
            MaxSpeed = 0.001f,
            InitialBoidCount = 25
        };

        var initializer = new BoidPopulationInitializer(settings, Limits);

        var population = initializer.CreateInitialPopulation(seed: 123);

        Assert.All(population, boid =>
        {
            var speed = boid.Velocity.Length();

            Assert.True(speed > 0f);
            Assert.True(speed <= settings.MaxSpeed);
        });
    }

    [Fact]
    public void CreateInitialPopulation_AllowsInitialCountEqualToMaximum()
    {
        var settings = DefaultSettings with
        {
            MaxBoids = 25,
            InitialBoidCount = 25
        };

        var initializer = new BoidPopulationInitializer(settings, Limits);

        var population = initializer.CreateInitialPopulation(seed: 123);

        Assert.Equal(settings.MaxBoids, population.Count);
    }

    [Fact]
    public void CreateInitialPopulation_RejectsInvalidSettings()
    {
        var settings = DefaultSettings with
        {
            InitialBoidCount = DefaultSettings.MaxBoids + 1
        };

        var initializer = new BoidPopulationInitializer(settings, Limits);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => initializer.CreateInitialPopulation(seed: 123));
    }
}
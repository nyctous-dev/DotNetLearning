
using System.Linq;
using Microsoft.Extensions.Configuration;
using BoidsSimulation.Core.Models;
using BoidsSimulation.Core.Simulation;

namespace BoidsSimulation.Core.Tests;

public sealed class SimulationStateTests
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

    private static IReadOnlyList<Boid> CreatePopulation(int seed = 12345)
    {
        var configuration = LoadConfiguration();

        var initializer = new BoidPopulationInitializer(
            configuration.Settings,
            configuration.Limits);

        return initializer.CreateInitialPopulation(seed);
    }

    [Fact]
    public void CreateInitialPopulation_AssignsUniqueSequentialIds()
    {
        var population = CreatePopulation();

        var actualIds = population.Select(boid => boid.Id);

        var expectedIds = Enumerable.Range(1, population.Count);

        Assert.Equal(expectedIds, actualIds);
        Assert.Equal(
            population.Count,
            actualIds.Distinct().Count());
    }

    [Fact]
    public void SimulationState_BoidCountMatchesPopulation()
    {
        var population = CreatePopulation();

        var state = new SimulationState(population);

        Assert.Equal(population.Count, state.BoidCount);
    }

    [Fact]
    public void TryGetBoid_ExistingIdReturnsCorrectBoid()
    {
        var population = CreatePopulation();
        var state = new SimulationState(population);

        var expectedBoid = population[0];

        var found = state.TryGetBoid(
            expectedBoid.Id,
            out var actualBoid);

        Assert.True(found);
        Assert.NotNull(actualBoid);

        Assert.Equal(expectedBoid.Id, actualBoid.Id);
        Assert.Equal(expectedBoid.Position, actualBoid.Position);
        Assert.Equal(expectedBoid.Velocity, actualBoid.Velocity);

        // Lookup should return the actual shared boid, not a separate copy.
        Assert.Same(expectedBoid, actualBoid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TryGetBoid_InvalidIdReturnsFalse(int id)
    {
        var state = new SimulationState(CreatePopulation());

        var found = state.TryGetBoid(id, out var boid);

        Assert.False(found);
        Assert.Null(boid);
    }

    [Fact]
    public void TryGetBoid_UnknownPositiveIdReturnsFalse()
    {
        var population = CreatePopulation();
        var state = new SimulationState(population);

        var unknownId = population.Count + 1;

        var found = state.TryGetBoid(unknownId, out var boid);

        Assert.False(found);
        Assert.Null(boid);
    }

    [Fact]
    public void SimulationState_EmptyPopulationHasZeroBoids()
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
        var state = new SimulationState(population);

        Assert.Equal(0, state.BoidCount);
        Assert.False(state.TryGetBoid(1, out var boid));
        Assert.Null(boid);
    }
}

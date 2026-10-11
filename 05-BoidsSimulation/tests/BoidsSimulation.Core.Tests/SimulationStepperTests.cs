
using System.Numerics;
using Microsoft.Extensions.Configuration;
using BoidsSimulation.Core.Models;
using BoidsSimulation.Core.Simulation;

namespace BoidsSimulation.Core.Tests;

public sealed class SimulationStepperTests
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

    private static (SimulationSettings Settings, SimulationLimits Limits)
        CreateTestConfiguration()
    {
        var configuration = LoadConfiguration();

        var settings = configuration.Settings with
        {
            WorldWidth = 1000,
            WorldHeight = 800,
            FixedTimeStep = 0.1f
        };

        return (settings, configuration.Limits);
    }

    private static Boid CreateBoid(
        int id,
        Vector2 position,
        Vector2 velocity)
    {
        return new Boid(id, position, velocity);
    }

    private static SimulationStepper CreateStepper(
        SimulationSettings settings,
        SimulationLimits limits)
    {
        return new SimulationStepper(settings, limits);
    }

    [Fact]
    public void Step_AdvancesPositionByVelocityTimesTimeStep()
    {
        var (settings, limits) = CreateTestConfiguration();
        var boid = CreateBoid(
            1,
            new Vector2(10f, 20f),
            new Vector2(30f, -40f));

        var state = new SimulationState(new[] { boid });
        var stepper = CreateStepper(settings, limits);

        stepper.Step(state);

        Assert.Equal(
            new Vector2(13f, 16f),
            boid.Position);
    }

    [Fact]
    public void Step_DoesNotChangeVelocity()
    {
        var (settings, limits) = CreateTestConfiguration();
        var originalVelocity = new Vector2(30f, -40f);
        var boid = CreateBoid(
            1,
            new Vector2(10f, 20f),
            originalVelocity);

        var state = new SimulationState(new[] { boid });
        var stepper = CreateStepper(settings, limits);

        stepper.Step(state);

        Assert.Equal(originalVelocity, boid.Velocity);
    }

    [Fact]
    public void Step_WrapsAcrossRightBoundary()
    {
        var (settings, limits) = CreateTestConfiguration();
        var boid = CreateBoid(
            1,
            new Vector2(499f, 0f),
            new Vector2(20f, 0f));

        var state = new SimulationState(new[] { boid });
        var stepper = CreateStepper(settings, limits);

        stepper.Step(state);

        Assert.Equal(-499f, boid.Position.X);
        Assert.Equal(0f, boid.Position.Y);
    }

    [Fact]
    public void Step_WrapsAcrossLeftBoundary()
    {
        var (settings, limits) = CreateTestConfiguration();
        var boid = CreateBoid(
            1,
            new Vector2(-499f, 0f),
            new Vector2(-20f, 0f));

        var state = new SimulationState(new[] { boid });
        var stepper = CreateStepper(settings, limits);

        stepper.Step(state);

        Assert.Equal(499f, boid.Position.X);
        Assert.Equal(0f, boid.Position.Y);
    }

    [Fact]
    public void Step_WrapsAcrossTopBoundary()
    {
        var (settings, limits) = CreateTestConfiguration();
        var boid = CreateBoid(
            1,
            new Vector2(0f, 399f),
            new Vector2(0f, 20f));

        var state = new SimulationState(new[] { boid });
        var stepper = CreateStepper(settings, limits);

        stepper.Step(state);

        Assert.Equal(0f, boid.Position.X);
        Assert.Equal(-399f, boid.Position.Y);
    }

    [Fact]
    public void Step_WrapsAcrossBottomBoundary()
    {
        var (settings, limits) = CreateTestConfiguration();
        var boid = CreateBoid(
            1,
            new Vector2(0f, -399f),
            new Vector2(0f, -20f));

        var state = new SimulationState(new[] { boid });
        var stepper = CreateStepper(settings, limits);

        stepper.Step(state);

        Assert.Equal(0f, boid.Position.X);
        Assert.Equal(399f, boid.Position.Y);
    }

    [Fact]
    public void Step_WrapsAcrossMultipleWorldWidths()
    {
        var (settings, limits) = CreateTestConfiguration();
        var boid = CreateBoid(
            1,
            new Vector2(0f, 0f),
            new Vector2(20050f, 0f));

        var state = new SimulationState(new[] { boid });
        var stepper = CreateStepper(settings, limits);

        stepper.Step(state);

        // Displacement is 2005 units in a 1000-unit-wide world.
        Assert.Equal(5f, boid.Position.X);
        Assert.Equal(0f, boid.Position.Y);
    }

    [Fact]
    public void Step_WrapsBothAxesInSameStep()
    {
        var (settings, limits) = CreateTestConfiguration();
        var boid = CreateBoid(
            1,
            new Vector2(499f, 399f),
            new Vector2(20f, 20f));

        var state = new SimulationState(new[] { boid });
        var stepper = CreateStepper(settings, limits);

        stepper.Step(state);

        Assert.Equal(-499f, boid.Position.X);
        Assert.Equal(-399f, boid.Position.Y);
    }

    [Fact]
    public void Step_LeavesPositionInsideBoundsWhenNoWrappingIsNeeded()
    {
        var (settings, limits) = CreateTestConfiguration();
        var boid = CreateBoid(
            1,
            new Vector2(100f, -100f),
            new Vector2(10f, 20f));

        var state = new SimulationState(new[] { boid });
        var stepper = CreateStepper(settings, limits);

        stepper.Step(state);

        Assert.Equal(new Vector2(101f, -98f), boid.Position);
    }

    [Fact]
    public void Step_HandlesEmptyPopulation()
    {
        var (settings, limits) = CreateTestConfiguration();
        var state = new SimulationState(Array.Empty<Boid>());
        var stepper = CreateStepper(settings, limits);

        var exception = Record.Exception(() => stepper.Step(state));

        Assert.Null(exception);
        Assert.Equal(0, state.BoidCount);
    }

    [Fact]
    public void Constructor_RejectsInvalidSettings()
    {
        var (settings, limits) = CreateTestConfiguration();

        var invalidSettings = settings with
        {
            FixedTimeStep = float.NaN
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            CreateStepper(invalidSettings, limits);
        });
    }
}

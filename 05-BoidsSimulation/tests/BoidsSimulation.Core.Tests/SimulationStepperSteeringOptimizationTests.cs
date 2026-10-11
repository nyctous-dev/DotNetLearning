using System.Numerics;
using BoidsSimulation.Core.Models;
using BoidsSimulation.Core.Simulation;
using Microsoft.Extensions.Configuration;

namespace BoidsSimulation.Core.Tests;

public sealed class SimulationStepperSteeringOptimizationTests
{
    private sealed record TestConfiguration(
        SimulationSettings Settings,
        SimulationLimits Limits);

    [Fact]
    public void Step_AlignmentWithMultipleNeighborsPreservesExpectedVelocity()
    {
        var configuration = LoadConfiguration();
        var settings = CreateSettings(
            configuration.Settings,
            activeRule: "alignment");

        var boid = CreateBoid(
            id: 1,
            position: Vector2.Zero,
            velocity: new Vector2(1f, 0f));

        var neighborA = CreateBoid(
            id: 2,
            position: new Vector2(1f, 0f),
            velocity: new Vector2(0f, 5f));

        var neighborB = CreateBoid(
            id: 3,
            position: new Vector2(2f, 0f),
            velocity: new Vector2(0f, 5f));

        var state = new SimulationState(
            new[] { boid, neighborA, neighborB });

        var stepper = new SimulationStepper(
            settings,
            configuration.Limits);

        stepper.Step(state);

        Assert.Equal(0.9f, boid.Velocity.X, 5);
        Assert.Equal(1f, boid.Velocity.Y, 5);
    }

    [Fact]
    public void Step_AlignmentWithCancellingNeighborsPreservesCurrentBehavior()
    {
        var configuration = LoadConfiguration();
        var settings = CreateSettings(
            configuration.Settings,
            activeRule: "alignment");

        var boid = CreateBoid(
            id: 1,
            position: Vector2.Zero,
            velocity: new Vector2(2f, 0f));

        var neighborA = CreateBoid(
            id: 2,
            position: new Vector2(1f, 0f),
            velocity: new Vector2(0f, 5f));

        var neighborB = CreateBoid(
            id: 3,
            position: new Vector2(-1f, 0f),
            velocity: new Vector2(0f, -5f));

        var state = new SimulationState(
            new[] { boid, neighborA, neighborB });

        var stepper = new SimulationStepper(
            settings,
            configuration.Limits);

        stepper.Step(state);

        // The neighbors' average velocity is zero, so alignment's
        // desired velocity is zero and the current velocity is damped.
        Assert.Equal(1.8f, boid.Velocity.X, 5);
        Assert.Equal(0f, boid.Velocity.Y, 5);
    }

    [Fact]
    public void Step_SeparationWithMultipleNeighborsPreservesExpectedDirection()
    {
        var configuration = LoadConfiguration();
        var settings = CreateSettings(
            configuration.Settings,
            activeRule: "separation");

        var boid = CreateBoid(
            id: 1,
            position: Vector2.Zero,
            velocity: Vector2.Zero);

        var neighborA = CreateBoid(
            id: 2,
            position: new Vector2(1f, 0f),
            velocity: Vector2.Zero);

        var neighborB = CreateBoid(
            id: 3,
            position: new Vector2(0f, 2f),
            velocity: Vector2.Zero);

        var state = new SimulationState(
            new[] { boid, neighborA, neighborB });

        var stepper = new SimulationStepper(
            settings,
            configuration.Limits);

        stepper.Step(state);

        Assert.Equal(-0.894427f, boid.Velocity.X, 4);
        Assert.Equal(-0.447214f, boid.Velocity.Y, 4);
    }

    [Fact]
    public void Step_CohesionWithMultipleNeighborsPreservesExpectedDirection()
    {
        var configuration = LoadConfiguration();
        var settings = CreateSettings(
            configuration.Settings,
            activeRule: "cohesion");

        var boid = CreateBoid(
            id: 1,
            position: Vector2.Zero,
            velocity: Vector2.Zero);

        var neighborA = CreateBoid(
            id: 2,
            position: new Vector2(1f, 0f),
            velocity: Vector2.Zero);

        var neighborB = CreateBoid(
            id: 3,
            position: new Vector2(0f, 2f),
            velocity: Vector2.Zero);

        var state = new SimulationState(
            new[] { boid, neighborA, neighborB });

        var stepper = new SimulationStepper(
            settings,
            configuration.Limits);

        stepper.Step(state);

        Assert.Equal(0.447214f, boid.Velocity.X, 4);
        Assert.Equal(0.894427f, boid.Velocity.Y, 4);
    }

    private static SimulationSettings CreateSettings(
        SimulationSettings baseline,
        string activeRule)
    {
        return baseline with
        {
            InitialBoidCount = 0,

            SeparationRadius =
                activeRule == "separation" ? 10f : 0f,

            AlignmentRadius =
                activeRule == "alignment" ? 10f : 0f,

            CohesionRadius =
                activeRule == "cohesion" ? 10f : 0f,

            SeparationWeight =
                activeRule == "separation" ? 1f : 0f,

            AlignmentWeight =
                activeRule == "alignment" ? 1f : 0f,

            CohesionWeight =
                activeRule == "cohesion" ? 1f : 0f,

            CellSize = 10f,
            MaxSpeed = 10f,
            MaxSteeringForce = 100f,
            FixedTimeStep = 0.1f,
            MaxNeighborEvaluationsPerBoid = 50
        };
    }

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

    private static Boid CreateBoid(
        int id,
        Vector2 position,
        Vector2 velocity)
    {
        return new Boid(id, position, velocity);
    }
}
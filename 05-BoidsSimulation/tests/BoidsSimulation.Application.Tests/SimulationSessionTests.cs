using System.Numerics;
using BoidsSimulation.Application.Sessions;
using BoidsSimulation.Application.Settings;
using BoidsSimulation.Core.Models;
using BoidsSimulation.Core.Simulation;

namespace BoidsSimulation.Application.Tests;

public sealed class SimulationSessionTests
{
    private static readonly SimulationSettings Settings = new()
    {
        WorldWidth = 100,
        WorldHeight = 80,
        MaxBoids = 10,
        InitialBoidCount = 1,

        SeparationRadius = 0f,
        AlignmentRadius = 0f,
        CohesionRadius = 0f,
        CellSize = 10f,

        SeparationWeight = 0f,
        AlignmentWeight = 0f,
        CohesionWeight = 0f,

        MaxSpeed = 10f,
        MaxSteeringForce = 5f,
        WanderStrength = 0f,
        AttractionForce = 5f,
        RepulsionForce = 5f,
        InteractionRadius = 10f,
        FixedTimeStep = 0.1f,

        MaxNeighborEvaluationsPerBoid = 50
    };

    private static readonly SimulationLimits Limits = CreateLimits();

    [Fact]
    public void GetSnapshot_ReturnsCurrentPopulation()
    {
        // Arrange
        var boid = new Boid(
            1,
            new Vector2(10f, 20f),
            new Vector2(1f, 0f));

        var session = CreateSession(boid);

        // Act
        var snapshot = session.GetSnapshot();

        // Assert
        var snapshotBoid = Assert.Single(snapshot.Boids);

        Assert.Equal(1, snapshotBoid.Id);
        Assert.Equal(new Vector2(10f, 20f), snapshotBoid.Position);
        Assert.Equal(new Vector2(1f, 0f), snapshotBoid.Velocity);
    }

    [Fact]
    public void Advance_UpdatesSimulationWithoutChangingPreviousSnapshot()
    {
        // Arrange
        var boid = new Boid(
            1,
            Vector2.Zero,
            new Vector2(1f, 0f));

        var session = CreateSession(boid);
        var previousSnapshot = session.GetSnapshot();

        // Act
        session.Advance();

        var currentSnapshot = session.GetSnapshot();

        // Assert
        Assert.Equal(
            Vector2.Zero,
            previousSnapshot.Boids[0].Position);

        Assert.Equal(
            new Vector2(0.1f, 0f),
            currentSnapshot.Boids[0].Position);

        Assert.NotSame(previousSnapshot, currentSnapshot);
    }

    [Fact]
    public void GetBehaviorSettings_ReturnsCurrentSettings()
    {
        // Arrange
        var (session, _) = CreateSettingsAwareSession(
            new Boid(1, Vector2.Zero, Vector2.Zero));

        // Act
        var actual = session.GetBehaviorSettings();

        // Assert
        Assert.Equal(SimulationBehaviorSettings.From(Settings), actual);
    }

    [Fact]
    public void UpdateBehaviorSettings_UpdatesSettingsAndLiveStepper()
    {
        // Arrange
        var boid = new Boid(1, new Vector2(10f, 20f), Vector2.Zero);
        var (session, _) = CreateSettingsAwareSession(boid);
        var requested = SimulationBehaviorSettings.From(Settings) with
        {
            MaxSpeed = 0.1f,
            WanderStrength = 12.5f
        };

        // Act
        var updated = session.UpdateBehaviorSettings(requested);

        session.SetInteraction(
            "test-client",
            new Vector2(11f, 20f),
            BoidInteractionMode.Attract);
        session.Advance();
        var snapshot = session.GetSnapshot();

        // Assert
        Assert.Equal(requested, updated);
        Assert.Equal(requested, session.GetBehaviorSettings());
        Assert.InRange(
            snapshot.Boids[0].Velocity.Length(),
            0.099f,
            0.101f);
    }

    [Fact]
    public void UpdateBehaviorSettings_InvalidSettingsDoNotReplaceCurrentSettings()
    {
        // Arrange
        var (session, _) = CreateSettingsAwareSession(
            new Boid(1, Vector2.Zero, Vector2.Zero));
        var original = session.GetBehaviorSettings();
        var invalid = original with
        {
            MaxSpeed = 100_001f
        };

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => session.UpdateBehaviorSettings(invalid));

        Assert.Equal(original, session.GetBehaviorSettings());
    }

    [Fact]
    public void GetBehaviorSettings_ThrowsWhenNoSettingsStoreIsConfigured()
    {
        // Arrange
        var session = CreateSession(
            new Boid(1, Vector2.Zero, Vector2.Zero));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(
            () => session.GetBehaviorSettings());
    }

    private static SimulationSession CreateSession(params Boid[] boids)
    {
        var state = new SimulationState(boids);
        var stepper = new SimulationStepper(Settings, Limits);

        return new SimulationSession(state, stepper);
    }

    private static (SimulationSession Session, SimulationState State)
        CreateSettingsAwareSession(params Boid[] boids)
    {
        var state = new SimulationState(boids);
        var stepper = new SimulationStepper(Settings, Limits);
        var settingsStore = new SimulationSettingsStore(Settings, Limits);
        var session = new SimulationSession(state, stepper, settingsStore);

        return (session, state);
    }

    private static SimulationLimits CreateLimits()
    {
        var integerRange = new SimulationLimits.IntegerRange
        {
            Min = 0,
            Max = 100_000
        };

        var floatRange = new SimulationLimits.FloatRange
        {
            Min = 0f,
            Max = 100_000f
        };

        return new SimulationLimits
        {
            WorldWidth = integerRange,
            WorldHeight = integerRange,
            MaxBoids = integerRange,
            InitialBoidCount = integerRange,

            SeparationRadius = floatRange,
            AlignmentRadius = floatRange,
            CohesionRadius = floatRange,
            CellSize = new SimulationLimits.FloatRange
            {
                Min = 1f,
                Max = 100_000f
            },

            SeparationWeight = floatRange,
            AlignmentWeight = floatRange,
            CohesionWeight = floatRange,

            MaxSpeed = floatRange,
            MaxSteeringForce = floatRange,
            WanderStrength = floatRange,
            AttractionForce = floatRange,
            RepulsionForce = floatRange,
            InteractionRadius = floatRange,

            FixedTimeStep = new SimulationLimits.FloatRange
            {
                Min = 0.001f,
                Max = 0.1f
            },

            MaxNeighborEvaluationsPerBoid = integerRange
        };
    }
}
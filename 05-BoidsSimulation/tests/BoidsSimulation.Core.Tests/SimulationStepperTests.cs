using System.Numerics;
using BoidsSimulation.Core.Models;
using BoidsSimulation.Core.Simulation;

namespace BoidsSimulation.Core.Tests;

public sealed class SimulationStepperTests
{
    private static readonly SimulationSettings DefaultSettings = new()
    {
        WorldWidth = 100,
        WorldHeight = 80,
        MaxBoids = 100,
        InitialBoidCount = 0,

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
        FixedTimeStep = 0.1f,

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
    public void Step_AdvancesPositionByVelocityAndFixedTimeStep()
    {
        var boid = CreateBoid(
            id: 1,
            position: new Vector2(10f, 20f),
            velocity: new Vector2(10f, -20f));

        var state = CreateState(boid);
        var stepper = CreateStepper();

        stepper.Step(state);

        Assert.Equal(11f, boid.Position.X, 5);
        Assert.Equal(18f, boid.Position.Y, 5);
    }

    [Fact]
    public void Step_DoesNotChangeVelocity()
    {
        var initialVelocity = new Vector2(7f, -3f);
        var boid = CreateBoid(
            id: 1,
            position: Vector2.Zero,
            velocity: initialVelocity);

        var state = CreateState(boid);
        var stepper = CreateStepper();

        stepper.Step(state);

        Assert.Equal(initialVelocity, boid.Velocity);
    }

    [Fact]
    public void Step_WrapsPositionAcrossPositiveXBoundary()
    {
        var boid = CreateBoid(
            id: 1,
            position: new Vector2(49f, 0f),
            velocity: new Vector2(20f, 0f));

        var state = CreateState(boid);
        var stepper = CreateStepper();

        stepper.Step(state);

        Assert.Equal(-49f, boid.Position.X, 5);
        Assert.Equal(0f, boid.Position.Y, 5);
    }

    [Fact]
    public void Step_WrapsPositionAcrossNegativeXBoundary()
    {
        var boid = CreateBoid(
            id: 1,
            position: new Vector2(-49f, 0f),
            velocity: new Vector2(-20f, 0f));

        var state = CreateState(boid);
        var stepper = CreateStepper();

        stepper.Step(state);

        Assert.Equal(49f, boid.Position.X, 5);
        Assert.Equal(0f, boid.Position.Y, 5);
    }

    [Fact]
    public void Step_WrapsPositionAcrossPositiveYBoundary()
    {
        var boid = CreateBoid(
            id: 1,
            position: new Vector2(0f, 39f),
            velocity: new Vector2(0f, 20f));

        var state = CreateState(boid);
        var stepper = CreateStepper();

        stepper.Step(state);

        Assert.Equal(0f, boid.Position.X, 5);
        Assert.Equal(-39f, boid.Position.Y, 5);
    }

    [Fact]
    public void Step_WrapsPositionAcrossNegativeYBoundary()
    {
        var boid = CreateBoid(
            id: 1,
            position: new Vector2(0f, -39f),
            velocity: new Vector2(0f, -20f));

        var state = CreateState(boid);
        var stepper = CreateStepper();

        stepper.Step(state);

        Assert.Equal(0f, boid.Position.X, 5);
        Assert.Equal(39f, boid.Position.Y, 5);
    }

    [Fact]
    public void Step_WrapsBothAxesWhenBothBoundariesAreCrossed()
    {
        var boid = CreateBoid(
            id: 1,
            position: new Vector2(49f, 39f),
            velocity: new Vector2(20f, 20f));

        var state = CreateState(boid);
        var stepper = CreateStepper();

        stepper.Step(state);

        Assert.Equal(-49f, boid.Position.X, 5);
        Assert.Equal(-39f, boid.Position.Y, 5);
    }

    [Fact]
    public void Step_WrapsCoordinatesThatCrossMultipleWorldWidths()
    {
        var boid = CreateBoid(
            id: 1,
            position: Vector2.Zero,
            velocity: new Vector2(2500f, -850f));

        var state = CreateState(boid);
        var stepper = CreateStepper();

        stepper.Step(state);

        Assert.Equal(-50f, boid.Position.X, 5);
        Assert.Equal(-5f, boid.Position.Y, 5);
    }

    [Fact]
    public void Step_LeavesPositionInsideWorldWhenNoBoundaryIsCrossed()
    {
        var initialPosition = new Vector2(10f, 20f);
        var boid = CreateBoid(
            id: 1,
            position: initialPosition,
            velocity: new Vector2(10f, -5f));

        var state = CreateState(boid);
        var stepper = CreateStepper();

        stepper.Step(state);

        Assert.Equal(11f, boid.Position.X, 5);
        Assert.Equal(19.5f, boid.Position.Y, 5);
    }

    [Fact]
    public void Step_HandlesEmptyPopulation()
    {
        var state = new SimulationState(Array.Empty<Boid>());
        var stepper = CreateStepper();

        var exception = Record.Exception(() => stepper.Step(state));

        Assert.Null(exception);
        Assert.Equal(0, state.BoidCount);
    }

    [Fact]
    public void Constructor_RejectsInvalidSettings()
    {
        var invalidSettings = DefaultSettings with
        {
            CellSize = 0f
        };

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SimulationStepper(invalidSettings, Limits));
    }

    [Fact]
    public void Step_RejectsNullState()
    {
        var stepper = CreateStepper();

        Assert.Throws<ArgumentNullException>(
            () => stepper.Step(null!));
    }

    private static SimulationStepper CreateStepper(
        SimulationSettings? settings = null)
    {
        return new SimulationStepper(
            settings ?? DefaultSettings,
            Limits);
    }

    private static SimulationState CreateState(params Boid[] boids)
    {
        return new SimulationState(boids);
    }

    private static Boid CreateBoid(
        int id,
        Vector2 position,
        Vector2 velocity)
    {
        return new Boid(id, position, velocity);
    }
}
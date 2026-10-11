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
    public void Step_DoesNotChangeVelocity_WhenBoidHasNoNeighbors()
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


    [Fact]
    public void Step_SeparationSteersBoidAwayFromNearbyBoid()
    {
        var boid = CreateBoid(
            id: 1,
            position: Vector2.Zero,
            velocity: Vector2.Zero);

        var neighbor = CreateBoid(
            id: 2,
            position: new Vector2(5f, 0f),
            velocity: Vector2.Zero);

        var settings = DefaultSettings with
        {
            SeparationRadius = 10f,
            SeparationWeight = 1f,
            AlignmentWeight = 0f,
            CohesionWeight = 0f
        };

        var state = CreateState(boid, neighbor);
        var stepper = CreateStepper(settings);

        stepper.Step(state);

        Assert.True(
            boid.Velocity.X < 0f,
            "The boid should steer away from its neighbor on the positive X axis.");

        Assert.Equal(0f, boid.Velocity.Y, 5);
    }

    [Fact]
    public void Step_SeparationDoesNotAffectBoidOutsideSeparationRadius()
    {
        var initialVelocity = Vector2.Zero;

        var boid = CreateBoid(
            id: 1,
            position: Vector2.Zero,
            velocity: initialVelocity);

        var distantBoid = CreateBoid(
            id: 2,
            position: new Vector2(11f, 0f),
            velocity: Vector2.Zero);

        var settings = DefaultSettings with
        {
            SeparationRadius = 10f,
            SeparationWeight = 1f,
            AlignmentWeight = 0f,
            CohesionWeight = 0f
        };

        var state = CreateState(boid, distantBoid);
        var stepper = CreateStepper(settings);

        stepper.Step(state);

        Assert.Equal(initialVelocity, boid.Velocity);
    }

    [Fact]
    public void Step_SeparationRespectsMaximumSteeringForce()
    {
        var boid = CreateBoid(
            id: 1,
            position: Vector2.Zero,
            velocity: Vector2.Zero);

        var neighbor = CreateBoid(
            id: 2,
            position: new Vector2(5f, 0f),
            velocity: Vector2.Zero);

        var settings = DefaultSettings with
        {
            SeparationRadius = 10f,
            SeparationWeight = 1f,
            AlignmentWeight = 0f,
            CohesionWeight = 0f,
            MaxSteeringForce = 2f,
            FixedTimeStep = 0.1f
        };

        var state = CreateState(boid, neighbor);
        var stepper = CreateStepper(settings);

        stepper.Step(state);

        var maximumVelocityChange =
            settings.MaxSteeringForce * settings.FixedTimeStep;

        Assert.True(
            boid.Velocity.Length() <= maximumVelocityChange + 0.00001f,
            $"Velocity change {boid.Velocity.Length()} exceeded the expected limit " +
            $"{maximumVelocityChange}.");
    }

    [Fact]
    public void Step_SeparationSteersBothBoidsAwayFromEachOther()
    {
        var boid = CreateBoid(
            id: 1,
            position: Vector2.Zero,
            velocity: Vector2.Zero);

        var neighbor = CreateBoid(
            id: 2,
            position: new Vector2(5f, 0f),
            velocity: Vector2.Zero);

        var settings = DefaultSettings with
        {
            SeparationRadius = 10f,
            SeparationWeight = 1f,
            AlignmentWeight = 0f,
            CohesionWeight = 0f
        };

        var state = CreateState(boid, neighbor);
        var stepper = CreateStepper(settings);

        stepper.Step(state);

        Assert.True(
            boid.Velocity.X < 0f,
            "The first boid should steer in the negative X direction.");

        Assert.True(
            neighbor.Velocity.X > 0f,
            "The second boid should steer in the positive X direction.");

        Assert.Equal(0f, boid.Velocity.Y, 5);
        Assert.Equal(0f, neighbor.Velocity.Y, 5);
    }

    [Fact]
    public void Step_AlignmentSteersBoidTowardAverageNeighborVelocity()
    {
        var boid = CreateBoid(
            id: 1,
            position: Vector2.Zero,
            velocity: new Vector2(2f, 0f));

        var neighbor = CreateBoid(
            id: 2,
            position: new Vector2(5f, 0f),
            velocity: new Vector2(0f, 4f));

        var settings = DefaultSettings with
        {
            SeparationRadius = 0f,
            AlignmentRadius = 20f,
            CohesionRadius = 0f,
            SeparationWeight = 0f,
            AlignmentWeight = 1f,
            CohesionWeight = 0f,
            MaxSteeringForce = 5f,
            MaxSpeed = 10f
        };

        var state = CreateState(boid, neighbor);
        var stepper = CreateStepper(settings);

        stepper.Step(state);

        Assert.True(
            boid.Velocity.Y > 0f,
            "The boid should steer toward the neighbor's positive Y heading.");

        Assert.True(
            boid.Velocity.X < 2f,
            "The boid should adjust its heading toward the neighbor's velocity.");

        Assert.True(boid.Velocity.Length() <= settings.MaxSpeed);
    }

    [Fact]
    public void Step_AlignmentIgnoresBoidsOutsideAlignmentRadius()
    {
        var initialVelocity = new Vector2(2f, 0f);

        var boid = CreateBoid(
            id: 1,
            position: Vector2.Zero,
            velocity: initialVelocity);

        var distantBoid = CreateBoid(
            id: 2,
            position: new Vector2(21f, 0f),
            velocity: new Vector2(0f, 4f));

        var settings = DefaultSettings with
        {
            SeparationRadius = 0f,
            AlignmentRadius = 20f,
            CohesionRadius = 0f,
            SeparationWeight = 0f,
            AlignmentWeight = 1f,
            CohesionWeight = 0f
        };

        var state = CreateState(boid, distantBoid);
        var stepper = CreateStepper(settings);

        stepper.Step(state);

        Assert.Equal(initialVelocity, boid.Velocity);
    }

    [Fact]
    public void Step_AlignmentDoesNotChangeVelocityWhenThereAreNoNeighbors()
    {
        var initialVelocity = new Vector2(2f, 0f);

        var boid = CreateBoid(
            id: 1,
            position: Vector2.Zero,
            velocity: initialVelocity);

        var settings = DefaultSettings with
        {
            SeparationRadius = 0f,
            AlignmentRadius = 20f,
            CohesionRadius = 0f,
            SeparationWeight = 0f,
            AlignmentWeight = 1f,
            CohesionWeight = 0f
        };

        var state = CreateState(boid);
        var stepper = CreateStepper(settings);

        stepper.Step(state);

        Assert.Equal(initialVelocity, boid.Velocity);
    }

    [Fact]
    public void Step_CohesionSteersBoidTowardAverageNeighborPosition()
    {
        var boid = CreateBoid(
            id: 1,
            position: Vector2.Zero,
            velocity: Vector2.Zero);

        var neighbor = CreateBoid(
            id: 2,
            position: new Vector2(10f, 0f),
            velocity: Vector2.Zero);

        var settings = DefaultSettings with
        {
            SeparationRadius = 0f,
            AlignmentRadius = 0f,
            CohesionRadius = 20f,
            SeparationWeight = 0f,
            AlignmentWeight = 0f,
            CohesionWeight = 1f
        };

        var state = CreateState(boid, neighbor);
        var stepper = CreateStepper(settings);

        stepper.Step(state);

        Assert.True(
            boid.Velocity.X > 0f,
            "The boid should steer toward its neighbor on the positive X axis.");

        Assert.Equal(0f, boid.Velocity.Y, 5);
    }

    [Fact]
    public void Step_CohesionIgnoresBoidsOutsideCohesionRadius()
    {
        var initialVelocity = new Vector2(2f, 0f);

        var boid = CreateBoid(
            id: 1,
            position: Vector2.Zero,
            velocity: initialVelocity);

        var distantBoid = CreateBoid(
            id: 2,
            position: new Vector2(21f, 0f),
            velocity: Vector2.Zero);

        var settings = DefaultSettings with
        {
            SeparationRadius = 0f,
            AlignmentRadius = 0f,
            CohesionRadius = 20f,
            SeparationWeight = 0f,
            AlignmentWeight = 0f,
            CohesionWeight = 1f
        };

        var state = CreateState(boid, distantBoid);
        var stepper = CreateStepper(settings);

        stepper.Step(state);

        Assert.Equal(initialVelocity, boid.Velocity);
    }

    [Fact]
    public void Step_CohesionUsesWrappedDistanceAcrossWorldBoundary()
    {
        var boid = CreateBoid(
            id: 1,
            position: new Vector2(48f, 0f),
            velocity: Vector2.Zero);

        var neighbor = CreateBoid(
            id: 2,
            position: new Vector2(-48f, 0f),
            velocity: Vector2.Zero);

        var settings = DefaultSettings with
        {
            SeparationRadius = 0f,
            AlignmentRadius = 0f,
            CohesionRadius = 10f,
            SeparationWeight = 0f,
            AlignmentWeight = 0f,
            CohesionWeight = 1f
        };

        var state = CreateState(boid, neighbor);
        var stepper = CreateStepper(settings);

        stepper.Step(state);

        Assert.True(
            boid.Velocity.X > 0f,
            "The boid should move toward the neighbor across the positive world boundary.");
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
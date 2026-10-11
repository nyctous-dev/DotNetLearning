
using System.Diagnostics;
using System.Numerics;
using BoidsSimulation.Application.Sessions;
using BoidsSimulation.Core.Models;
using BoidsSimulation.Core.Simulation;
using BoidsSimulation.Web.Services;

namespace BoidsSimulation.Web.Tests;

public sealed class SimulationBackgroundServiceTests
{
    [Fact]
    public async Task StartAsync_AdvancesSimulationAndStopAsyncStopsIt()
    {
        var settings = CreateSettings();
        var limits = CreateLimits();

        var boid = new Boid(
            1,
            new Vector2(10f, 20f),
            new Vector2(1f, 0f));

        var state = new SimulationState([boid]);
        var stepper = new SimulationStepper(settings, limits);
        var session = new SimulationSession(state, stepper);
        var loop = new SimulationLoop(session, settings);
        var service = new SimulationBackgroundService(loop);

        await service.StartAsync(CancellationToken.None);

        bool advanced;

        try
        {
            advanced = await WaitForPositionAsync(
                session,
                targetX: 10.03f,
                timeout: TimeSpan.FromSeconds(2));
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }

        Assert.True(
            advanced,
            "The hosted service did not advance the simulation.");
    }

    private static async Task<bool> WaitForPositionAsync(
        SimulationSession session,
        float targetX,
        TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            if (session.GetSnapshot().Boids[0].Position.X >= targetX)
            {
                return true;
            }

            await Task.Delay(5);
        }

        return session.GetSnapshot().Boids[0].Position.X >= targetX;
    }

    private static SimulationSettings CreateSettings() => new()
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
        FixedTimeStep = 0.02f,

        MaxNeighborEvaluationsPerBoid = 50
    };

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
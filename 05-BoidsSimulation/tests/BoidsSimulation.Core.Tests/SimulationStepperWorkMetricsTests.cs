using System.Numerics;
using BoidsSimulation.Core.Models;
using BoidsSimulation.Core.Simulation;
using Microsoft.Extensions.Configuration;

namespace BoidsSimulation.Core.Tests;

public sealed class SimulationStepperWorkMetricsTests
{
    private sealed record TestConfiguration(
        SimulationSettings Settings,
        SimulationLimits Limits);

    [Fact]
    public void Step_ReportsNeighborWorkAndSteeringRuleMatches()
    {
        var configuration = LoadConfiguration();
        var settings = CreateSettings(configuration.Settings);

        var boidA = CreateBoid(
            1,
            new Vector2(0f, 0f),
            Vector2.Zero);

        var boidB = CreateBoid(
            2,
            new Vector2(1f, 0f),
            Vector2.Zero);

        var boidC = CreateBoid(
            3,
            new Vector2(2f, 0f),
            Vector2.Zero);

        var isolatedBoid = CreateBoid(
            4,
            new Vector2(100f, 0f),
            Vector2.Zero);

        var state = new SimulationState(
            new[] { boidA, boidB, boidC, isolatedBoid });

        var stepper = new SimulationStepper(
            settings,
            configuration.Limits);

        stepper.Step(state);

        var metrics = stepper.LastStepWorkMetrics;

        Assert.Equal(4, metrics.BoidCount);

        // The three clustered boids each receive three grid candidates,
        // including themselves. The isolated boid receives itself.
        Assert.Equal(10L, metrics.GridCandidatesReturned);

        // Each clustered boid examines the other two. The isolated boid
        // has no non-self candidates to examine.
        Assert.Equal(6L, metrics.NeighborCandidatesExamined);
        Assert.Equal(6L, metrics.SquaredDistanceCalculations);

        // Each of the six directed neighbor relationships falls within
        // all three steering radii.
        Assert.Equal(6L, metrics.SeparationMatches);
        Assert.Equal(6L, metrics.AlignmentMatches);
        Assert.Equal(6L, metrics.CohesionMatches);

        Assert.Equal(1, metrics.BoidsWithZeroEvaluatedNeighbors);
        Assert.Equal(0, metrics.BoidsAtEvaluationLimit);

        Assert.Equal(0, metrics.MinimumEvaluationsPerBoid);
        Assert.Equal(1.5, metrics.AverageEvaluationsPerBoid, 5);
        Assert.Equal(2, metrics.MedianEvaluationsPerBoid);
        Assert.Equal(2, metrics.P95EvaluationsPerBoid);
        Assert.Equal(2, metrics.MaximumEvaluationsPerBoid);
    }

    [Fact]
    public void Step_ReportsBoidsThatReachTheNeighborEvaluationLimit()
    {
        var configuration = LoadConfiguration();

        var settings = CreateSettings(
            configuration.Settings,
            maxNeighborEvaluationsPerBoid: 1);

        var boidA = CreateBoid(
            1,
            new Vector2(0f, 0f),
            Vector2.Zero);

        var boidB = CreateBoid(
            2,
            new Vector2(1f, 0f),
            Vector2.Zero);

        var boidC = CreateBoid(
            3,
            new Vector2(2f, 0f),
            Vector2.Zero);

        var state = new SimulationState(
            new[] { boidA, boidB, boidC });

        var stepper = new SimulationStepper(
            settings,
            configuration.Limits);

        stepper.Step(state);

        var metrics = stepper.LastStepWorkMetrics;

        Assert.Equal(3, metrics.BoidCount);
        Assert.Equal(3L, metrics.NeighborCandidatesExamined);
        Assert.Equal(3L, metrics.SquaredDistanceCalculations);

        Assert.Equal(3, metrics.BoidsAtEvaluationLimit);
        Assert.Equal(0, metrics.BoidsWithZeroEvaluatedNeighbors);

        Assert.Equal(1, metrics.MinimumEvaluationsPerBoid);
        Assert.Equal(1.0, metrics.AverageEvaluationsPerBoid, 5);
        Assert.Equal(1, metrics.MedianEvaluationsPerBoid);
        Assert.Equal(1, metrics.P95EvaluationsPerBoid);
        Assert.Equal(1, metrics.MaximumEvaluationsPerBoid);
    }

    [Fact]
    public void Step_ReportsZeroWorkForAnEmptyPopulation()
    {
        var configuration = LoadConfiguration();
        var settings = CreateSettings(configuration.Settings);

        var state = new SimulationState(Array.Empty<Boid>());

        var stepper = new SimulationStepper(
            settings,
            configuration.Limits);

        stepper.Step(state);

        var metrics = stepper.LastStepWorkMetrics;

        Assert.Equal(0, metrics.BoidCount);
        Assert.Equal(0L, metrics.GridCandidatesReturned);
        Assert.Equal(0L, metrics.NeighborCandidatesExamined);
        Assert.Equal(0L, metrics.SquaredDistanceCalculations);
        Assert.Equal(0L, metrics.SeparationMatches);
        Assert.Equal(0L, metrics.AlignmentMatches);
        Assert.Equal(0L, metrics.CohesionMatches);
        Assert.Equal(0, metrics.BoidsWithZeroEvaluatedNeighbors);
        Assert.Equal(0, metrics.BoidsAtEvaluationLimit);
        Assert.Equal(0.0, metrics.AverageEvaluationsPerBoid);
    }

    private static SimulationSettings CreateSettings(
        SimulationSettings baseline,
        int maxNeighborEvaluationsPerBoid = 50)
    {
        return baseline with
        {
            WorldWidth = 1000,
            WorldHeight = 1000,
            MaxBoids = 100,
            InitialBoidCount = 0,

            SeparationRadius = 10f,
            AlignmentRadius = 10f,
            CohesionRadius = 10f,

            SeparationWeight = 1f,
            AlignmentWeight = 1f,
            CohesionWeight = 1f,

            CellSize = 10f,
            MaxSpeed = 10f,
            MaxSteeringForce = 100f,
            FixedTimeStep = 0.001f,

            MaxNeighborEvaluationsPerBoid =
                maxNeighborEvaluationsPerBoid
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
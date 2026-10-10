
using Microsoft.Extensions.Logging;

public sealed class SimulationService
{
    private readonly ILogger<SimulationService> _logger;

    public SimulationService(ILogger<SimulationService> logger)
    {
        _logger = logger;
    }

    public void Run(int simulationId, int boidCount)
    {
        if (boidCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(boidCount),
                "Boid count must be greater than zero.");
        }

        using IDisposable? scope = _logger.BeginScope(
            "SimulationId: {SimulationId}",
            simulationId);

        _logger.LogDebug(
            "Preparing simulation {SimulationId} with {BoidCount} boids.",
            simulationId,
            boidCount);

        _logger.LogInformation(
            "Simulation {SimulationId} started with {BoidCount} boids.",
            simulationId,
            boidCount);

        if (boidCount > 10)
        {
            _logger.LogWarning(
                "Simulation {SimulationId} has {BoidCount} boids, exceeding the recommended limit of {RecommendedBoidCount}.",
                simulationId,
                boidCount,
                10);
        }

        try
        {
            UpdateSimulation();
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError(
                exception,
                "Simulation {SimulationId} failed while updating {BoidCount} boids.",
                simulationId,
                boidCount);

            throw;
        }
    }

    private static void UpdateSimulation()
    {
        throw new InvalidOperationException(
            "Controlled failure during simulation update.");
    }
}

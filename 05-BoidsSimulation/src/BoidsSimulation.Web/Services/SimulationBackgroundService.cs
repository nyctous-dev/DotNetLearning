
using BoidsSimulation.Application.Sessions;
using Microsoft.Extensions.Hosting;

namespace BoidsSimulation.Web.Services;

public sealed class SimulationBackgroundService : BackgroundService
{
    private readonly SimulationLoop _simulationLoop;

    public SimulationBackgroundService(SimulationLoop simulationLoop)
    {
        ArgumentNullException.ThrowIfNull(simulationLoop);

        _simulationLoop = simulationLoop;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _simulationLoop.RunAsync(stoppingToken);
    }
}

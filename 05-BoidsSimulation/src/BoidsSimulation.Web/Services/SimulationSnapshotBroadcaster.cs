
using BoidsSimulation.Application.Sessions;
using BoidsSimulation.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BoidsSimulation.Web.Services;

public sealed class SimulationSnapshotBroadcaster : BackgroundService
{
    private static readonly TimeSpan BroadcastInterval =
        TimeSpan.FromMilliseconds(1000.0 / 60.0);

    private readonly SimulationSession _session;
    private readonly IHubContext<SimulationHub> _hubContext;
    private readonly ILogger<SimulationSnapshotBroadcaster> _logger;

    public SimulationSnapshotBroadcaster(
        SimulationSession session,
        IHubContext<SimulationHub> hubContext,
        ILogger<SimulationSnapshotBroadcaster> logger)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(hubContext);
        ArgumentNullException.ThrowIfNull(logger);

        _session = session;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(BroadcastInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var snapshot = _session.GetSnapshot();

                await _hubContext.Clients
                    .All
                    .SendAsync(
                        "SimulationSnapshot",
                        snapshot,
                        stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "The simulation snapshot broadcaster stopped unexpectedly.");

            throw;
        }
    }
}

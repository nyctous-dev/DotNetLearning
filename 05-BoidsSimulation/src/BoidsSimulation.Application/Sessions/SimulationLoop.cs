
using BoidsSimulation.Core.Models;

namespace BoidsSimulation.Application.Sessions;

public sealed class SimulationLoop
{
    private readonly SimulationSession _session;
    private readonly TimeSpan _timeStep;

    public SimulationLoop(
        SimulationSession session,
        SimulationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(settings);

        if (!float.IsFinite(settings.FixedTimeStep) ||
            settings.FixedTimeStep <= 0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(settings),
                settings.FixedTimeStep,
                "The fixed time step must be a finite value greater than zero.");
        }

        _session = session;
        _timeStep = TimeSpan.FromSeconds(settings.FixedTimeStep);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_timeStep);

        while (await timer
            .WaitForNextTickAsync(cancellationToken)
            .ConfigureAwait(false))
        {
            _session.Advance();
        }
    }
}

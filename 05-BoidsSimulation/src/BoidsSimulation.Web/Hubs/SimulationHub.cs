
using BoidsSimulation.Application.Sessions;
using BoidsSimulation.Application.Snapshots;
using Microsoft.AspNetCore.SignalR;

namespace BoidsSimulation.Web.Hubs;

public sealed class SimulationHub : Hub
{
    private readonly SimulationSession _session;

    public SimulationHub(SimulationSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        _session = session;
    }

    public SimulationSnapshot GetSnapshot()
    {
        return _session.GetSnapshot();
    }
}

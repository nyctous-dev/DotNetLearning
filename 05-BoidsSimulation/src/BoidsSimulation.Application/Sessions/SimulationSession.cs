using BoidsSimulation.Application.Snapshots;
using BoidsSimulation.Core.Simulation;

namespace BoidsSimulation.Application.Sessions;

public sealed class SimulationSession
{
    private readonly object _syncRoot = new();
    private readonly SimulationState _state;
    private readonly SimulationStepper _stepper;

    public SimulationSession(
        SimulationState state,
        SimulationStepper stepper)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(stepper);

        _state = state;
        _stepper = stepper;
    }

    public SimulationSnapshot GetSnapshot()
    {
        lock (_syncRoot)
        {
            var copiedStates = _state.CopyBoidStates();

            return SimulationSnapshot.FromBoidStates(copiedStates);
        }
    }

    public void Advance()
    {
        lock (_syncRoot)
        {
            _stepper.Step(_state);
        }
    }
}
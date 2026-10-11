using System.Collections.ObjectModel;
using BoidsSimulation.Core.Simulation;

namespace BoidsSimulation.Application.Snapshots;

public readonly record struct BoidSnapshot(
    int Id,
    System.Numerics.Vector2 Position,
    System.Numerics.Vector2 Velocity);

public sealed class SimulationSnapshot
{
    private readonly ReadOnlyCollection<BoidSnapshot> _boids;

    private SimulationSnapshot(BoidSnapshot[] boids)
    {
        _boids = Array.AsReadOnly(boids);
    }

    public IReadOnlyList<BoidSnapshot> Boids => _boids;

    public static SimulationSnapshot FromBoidStates(
        IReadOnlyList<BoidStateSnapshot> states)
    {
        ArgumentNullException.ThrowIfNull(states);

        var boidSnapshots = new BoidSnapshot[states.Count];

        for (var i = 0; i < states.Count; i++)
        {
            var state = states[i];

            boidSnapshots[i] = new BoidSnapshot(
                state.Id,
                state.Position,
                state.Velocity);
        }

        return new SimulationSnapshot(boidSnapshots);
    }
}
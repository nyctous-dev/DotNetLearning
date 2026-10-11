using BoidsSimulation.Core.Models;

namespace BoidsSimulation.Core.Simulation;

public sealed class SimulationState
{
    private readonly Dictionary<int, Boid> _boidsById;

    public int BoidCount => _boidsById.Count;

    internal IReadOnlyCollection<Boid> Boids => _boidsById.Values;

    public SimulationState(IReadOnlyList<Boid> population)
    {
        ArgumentNullException.ThrowIfNull(population);

        _boidsById = new Dictionary<int, Boid>(population.Count);

        foreach (var boid in population)
        {
            ArgumentNullException.ThrowIfNull(boid);

            if (boid.Id <= 0)
            {
                throw new ArgumentException(
                    $"Boid ID must be positive, but received {boid.Id}.",
                    nameof(population));
            }

            if (!_boidsById.TryAdd(boid.Id, boid))
            {
                throw new ArgumentException(
                    $"Duplicate boid ID encountered: {boid.Id}.",
                    nameof(population));
            }
        }
    }

    public bool TryGetBoid(int id, out Boid? boid)
    {
        if (id > 0 && _boidsById.TryGetValue(id, out var foundBoid))
        {
            boid = foundBoid;
            return true;
        }

        boid = null;
        return false;
    }

    public BoidStateSnapshot[] CopyBoidStates()
    {
        var copiedStates = new BoidStateSnapshot[_boidsById.Count];
        var index = 0;

        foreach (var boid in _boidsById.Values)
        {
            copiedStates[index++] = new BoidStateSnapshot(
                boid.Id,
                boid.Position,
                boid.Velocity);
        }

        return copiedStates;
    }
}
using System.Collections.Concurrent;
using DataTransferObjectsAndValidation.Models;

namespace DataTransferObjectsAndValidation.Services;

public sealed class SimulationStore
{
    private readonly ConcurrentDictionary<Guid, Simulation> _simulations = new();

    public void Add(Simulation simulation) =>
        _simulations.TryAdd(simulation.Id, simulation);

    public Simulation? Find(Guid id) =>
        _simulations.GetValueOrDefault(id);
}
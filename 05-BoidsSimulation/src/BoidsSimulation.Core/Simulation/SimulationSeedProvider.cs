
namespace BoidsSimulation.Core.Simulation;

public sealed class SimulationSeedProvider
{
    public int Seed { get; }

    public SimulationSeedProvider(int? configuredSeed)
    {
        Seed = configuredSeed ?? Random.Shared.Next();
    }
}

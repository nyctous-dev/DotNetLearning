
namespace BoidsSimulation.Core.Models;

public sealed class SimulationSettings
{
    // World configuration
    public int WorldWidth { get; init; } = 10_000;
    public int WorldHeight { get; init; } = 10_000;

    // Population limits
    public int MaxBoids { get; init; } = 10_000;

    // Flocking behavior
    public float SeparationRadius { get; init; } = 25f;
    public float AlignmentRadius { get; init; } = 50f;
    public float CohesionRadius { get; init; } = 50f;

    public float SeparationWeight { get; init; } = 1.5f;
    public float AlignmentWeight { get; init; } = 1f;
    public float CohesionWeight { get; init; } = 1f;

    // Movement constraints
    public float MaxSpeed { get; init; } = 100f;
    public float MaxSteeringForce { get; init; } = 50f;

    // User interaction forces
    public float AttractionForce { get; init; } = 100f;
    public float RepulsionForce { get; init; } = 150f;
    public float InteractionRadius { get; init; } = 100f;

    // Simulation timing
    public float FixedTimeStep { get; init; } = 1f / 60f;
}

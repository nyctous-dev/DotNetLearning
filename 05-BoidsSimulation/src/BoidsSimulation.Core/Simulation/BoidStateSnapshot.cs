using System.Numerics;

namespace BoidsSimulation.Core.Simulation;

public readonly record struct BoidStateSnapshot(
    int Id,
    Vector2 Position,
    Vector2 Velocity);
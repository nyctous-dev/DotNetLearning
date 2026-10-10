
using System.Numerics;

namespace BoidsSimulation.Core.Models;

public sealed class Boid
{
    public Vector2 Position { get; set; }

    public Vector2 Velocity { get; set; }

    public Boid(Vector2 position, Vector2 velocity)
    {
        Position = position;
        Velocity = velocity;
    }
}

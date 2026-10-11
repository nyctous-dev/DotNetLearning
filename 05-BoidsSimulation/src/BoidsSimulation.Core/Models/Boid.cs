
using System.Numerics;

namespace BoidsSimulation.Core.Models;

public sealed class Boid
{
    public int Id { get; }

    public Vector2 Position { get; set; }

    public Vector2 Velocity { get; set; }

    public Boid(int id, Vector2 position, Vector2 velocity)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                id,
                "Boid ID must be a positive integer.");
        }

        Id = id;
        Position = position;
        Velocity = velocity;
    }
}

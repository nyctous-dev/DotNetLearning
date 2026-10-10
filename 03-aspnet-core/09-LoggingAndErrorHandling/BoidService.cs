using Microsoft.Extensions.Logging;

public sealed class BoidService(ILogger<BoidService> logger)
{
    private static readonly Dictionary<int, Boid> Boids = new()
    {
        [1] = new Boid(1, 2.0),
        [2] = new Boid(2, 2.5),
        [3] = new Boid(3, 1.5)
    };

    public Boid? FindById(int id)
    {
        logger.LogDebug("Looking up boid {BoidId}", id);

        if (Boids.TryGetValue(id, out Boid? boid))
        {
            logger.LogInformation(
                "Boid {BoidId} retrieved with speed {MovementSpeed}",
                boid.Id,
                boid.Speed);

            return boid;
        }

        logger.LogWarning("Boid {BoidId} was not found", id);
        return null;
    }
}
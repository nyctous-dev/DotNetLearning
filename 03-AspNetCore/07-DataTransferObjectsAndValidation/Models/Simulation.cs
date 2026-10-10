namespace DataTransferObjectsAndValidation.Models;

public sealed class Simulation
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; init; }

    public int BoidCount { get; init; }

    public bool IsRunning { get; init; }

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
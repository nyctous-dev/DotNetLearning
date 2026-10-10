namespace DataTransferObjectsAndValidation.Dtos;

public sealed class SimulationResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public int BoidCount { get; init; }

    public bool IsRunning { get; init; }
}
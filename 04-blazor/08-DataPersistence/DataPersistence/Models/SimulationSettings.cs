namespace BlazorPersistence.Models;

public sealed class SimulationSettings
{
    public int BoidCount { get; set; } = 100;

    public double MovementSpeed { get; set; } = 2.0;

    public bool ShowTrails { get; set; }

    public bool HasValidValues() =>
        BoidCount >= 0 &&
        double.IsFinite(MovementSpeed) &&
        MovementSpeed >= 0;
}
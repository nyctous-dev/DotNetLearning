public sealed class SimulationConfig
{
    public int BoidCount { get; set; } = 100;
    public double MovementSpeed { get; set; } = 2.0;
    public bool IsRunning { get; set; }

    public static SimulationConfig CreateDefault() => new();
}
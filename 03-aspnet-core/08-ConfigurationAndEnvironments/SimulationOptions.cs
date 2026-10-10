public sealed class SimulationOptions
{
    public const string SectionName = "Simulation";

    public int MaxBoids { get; set; } = 500;

    public double DefaultSpeed { get; set; } = 2.0;
}
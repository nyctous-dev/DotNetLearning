
double distance = SimulationCalculations.CalculateDistance(
    x1: 10.0,
    y1: 20.0,
    x2: 13.0,
    y2: 24.0);

Console.WriteLine($"Distance: {distance:F2}");

double baseSpeed = 2.0;

double movementSpeed = SimulationCalculations.CalculateSpeed(
    baseSpeed,
    multiplier: 1.5);

Console.WriteLine($"Movement speed: {movementSpeed:F2}");

SimulationCalculations.PrintSimulationStatus("Boids", isRunning: true);

double boostedSpeed = SimulationCalculations.CalculateSpeed(
    baseSpeed,
    multiplier: 1.5,
    boost: 2.0);

Console.WriteLine($"Boosted speed: {boostedSpeed:F2}");

double defaultSpeed = SimulationCalculations.CalculateSpeed(baseSpeed);

Console.WriteLine($"Default speed: {defaultSpeed:F2}");

SimulationCalculations.PrintSimulationStatus(
    name: "Boids",
    isRunning: false);

int boidCount = 100;

SimulationCalculations.TryIncreaseCount(boidCount);

Console.WriteLine($"Boid count after method call: {boidCount}");

static class SimulationCalculations
{
    private const double DefaultSpeedMultiplier = 1.0;

    public static double CalculateDistance(
        double x1,
        double y1,
        double x2,
        double y2)
    {
        double deltaX = x2 - x1;
        double deltaY = y2 - y1;

        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    // Overload 1: supports an optional multiplier.
    public static double CalculateSpeed(
        double baseSpeed,
        double multiplier = DefaultSpeedMultiplier)
    {
        return baseSpeed * multiplier;
    }

    // Overload 2: adds a boost multiplier.
    public static double CalculateSpeed(
        double baseSpeed,
        double multiplier,
        double boost)
    {
        return baseSpeed * multiplier * boost;
    }

    public static void PrintSimulationStatus(string name, bool isRunning)
    {
        string status = isRunning ? "Running" : "Stopped";

        Console.WriteLine($"Simulation '{name}': {status}");
    }

    public static void TryIncreaseCount(int count)
    {
        count++;
        Console.WriteLine($"Count inside method: {count}");
    }
}

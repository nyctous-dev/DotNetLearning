
string validCountInput = "3";
string invalidCountInput = "many";

Console.WriteLine("=== Parse boid count ===");

if (int.TryParse(validCountInput, out int boidCount))
{
    SimulationConfiguration.ValidateBoidCount(boidCount);
    Console.WriteLine($"Parsed boid count: {boidCount}");
}

if (!int.TryParse(invalidCountInput, out _))
{
    Console.WriteLine(
        $"Invalid count input: '{invalidCountInput}'. Enter a valid integer.");
}

Console.WriteLine("\n=== Validate configuration ===");

try
{
    SimulationConfiguration.ValidateBoidCount(0);
}
catch (ArgumentOutOfRangeException)
{
    Console.WriteLine("Rejected invalid boid count.");
}
finally
{
    Console.WriteLine("Boid-count validation finished.");
}

double speed = 2.5;

SimulationConfiguration.ValidateSpeed(speed);
Console.WriteLine($"Accepted speed: {speed:F1}");

try
{
    SimulationConfiguration.ValidateSpeed(double.NaN);
}
catch (ArgumentOutOfRangeException)
{
    Console.WriteLine("Rejected invalid speed.");
}
finally
{
    Console.WriteLine("Speed validation finished.");
}

Console.WriteLine("\n=== Exception propagation ===");

try
{
    SimulationRunner.RunStep(isRunning: false);
}
catch (InvalidOperationException exception)
    when (exception.Message.Contains("not running", StringComparison.Ordinal))
{
    Console.WriteLine($"Handled at caller: {exception.Message}");
}

static class SimulationConfiguration
{
    public static void ValidateBoidCount(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                "Boid count must be greater than zero.");
        }
    }

    public static void ValidateSpeed(double speed)
    {
        if (!double.IsFinite(speed) || speed < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(speed),
                "Speed must be finite and non-negative.");
        }
    }
}

static class SimulationRunner
{
    public static void RunStep(bool isRunning)
    {
        try
        {
            if (!isRunning)
            {
                throw new InvalidOperationException(
                    "The simulation is not running.");
            }

            Console.WriteLine("Simulation step completed.");
        }
        catch (InvalidOperationException exception) when (!isRunning)
        {
            Console.WriteLine(
                $"Recording simulation failure: {exception.Message}");

            throw;
        }
        finally
        {
            Console.WriteLine("Simulation step cleanup complete.");
        }
    }
}

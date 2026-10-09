
string simulationName = "Boids";
const int MaxBoidCount = 500;

int boidCount = 100;
double movementSpeed = 2.5;
bool isRunning = false;

// Type inference
var simulationVersion = 1;

// Numeric conversion: int to double
double preciseBoidCount = boidCount;

// Parse text safely
string userInput = "150";

if (int.TryParse(userInput, out int requestedBoidCount))
{
    boidCount = requestedBoidCount;
}
else
{
    Console.WriteLine($"Invalid boid count: '{userInput}'. Keeping the default.");
}

// Nullable value and fallback
int? selectedBoidCount = null;
int effectiveBoidCount = selectedBoidCount ?? boidCount;

Console.WriteLine($"Simulation: {simulationName}");
Console.WriteLine($"Version: {simulationVersion}");
Console.WriteLine($"Boid count: {effectiveBoidCount}");
Console.WriteLine($"Maximum boids: {MaxBoidCount}");
Console.WriteLine($"Movement speed: {movementSpeed}");
Console.WriteLine($"Running: {isRunning}");
Console.WriteLine($"Converted count: {preciseBoidCount}");

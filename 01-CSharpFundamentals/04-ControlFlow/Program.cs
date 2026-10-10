
const int MaxBoids = 500;

// 1. Classify the simulation size.
int boidCount = 120;

string simulationSize = boidCount switch
{
    0 => "Empty",
    < 0 => "Invalid",
    < 50 => "Small",
    < 200 => "Medium",
    _ => "Large"
};

Console.WriteLine($"Simulation size: {simulationSize}");

// 2. Translate the display mode.
char displayMode = 'A';

string displayDescription = displayMode switch
{
    'A' => "Displaying all boids",
    'S' => "Displaying selected boids",
    'H' => "Displaying hidden boids",
    _ => "Unknown display mode"
};

Console.WriteLine($"Display mode: {displayDescription}");

// 3. Simulate 10 numbered steps.
Console.WriteLine("\nSimulation steps:");

for (int step = 1; step <= 10; step++)
{
    Console.WriteLine($"Step {step}");
}

// 4. Count down using a while loop.
Console.WriteLine("\nCountdown:");

int countdown = 3;

while (countdown > 0)
{
    Console.WriteLine(countdown);
    countdown--;
}

// 5. Demonstrate a do-while loop.
Console.WriteLine("\nInitialization:");

int initializationAttempts = 0;

do
{
    initializationAttempts++;
    Console.WriteLine($"Initialization attempt {initializationAttempts}");
}
while (initializationAttempts < 1);

// 6–7. Process counts using foreach, continue, and break.
Console.WriteLine("\nValidating boid counts:");

int[] proposedCounts = { 0, 25, 50, 120, 600, 100 };

foreach (int count in proposedCounts)
{
    if (count == 0)
    {
        Console.WriteLine("Skipping empty simulation.");
        continue;
    }

    if (count > MaxBoids)
    {
        Console.WriteLine(
            $"Count {count} exceeds the maximum of {MaxBoids}. Stopping.");
        break;
    }

    Console.WriteLine($"Accepted boid count: {count}");
}


const int MaxBoidCount = 500;

int boidCount = 100;
double baseSpeed = 2.0;
double speedMultiplier = 1.5;
bool isRunning = true;
bool isPaused = false;

// 1. Arithmetic: calculate movement speed.
double movementSpeed = baseSpeed * speedMultiplier;

// 2. Compound assignment: add boids.
boidCount += 25;

// 3. Comparison: check capacity.
bool isBelowMaximum = boidCount < MaxBoidCount;

// 4. Logical operators: determine whether simulation should update.
bool shouldUpdate =
    isRunning &&
    !isPaused &&
    boidCount > 0;

// 5. Remainder: determine whether the count is even.
bool hasEvenBoidCount = boidCount % 2 == 0;

// 6. Multiple operators with explicit precedence.
int remainingCapacity = MaxBoidCount - (boidCount + 25);

// Display results.
Console.WriteLine($"Boid count: {boidCount}");
Console.WriteLine($"Maximum boid count: {MaxBoidCount}");
Console.WriteLine($"Movement speed: {movementSpeed}");
Console.WriteLine($"Below maximum: {isBelowMaximum}");
Console.WriteLine($"Should update: {shouldUpdate}");
Console.WriteLine($"Boid count is even: {hasEvenBoidCount}");
Console.WriteLine($"Remaining capacity after adding 25: {remainingCapacity}");

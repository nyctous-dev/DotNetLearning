
List<Boid> boids =
[
    new(1, 10.0, 20.0, 1.2),
    new(2, 30.0, 40.0, 2.5),
    new(3, 50.0, 60.0, 3.0),
    new(4, 15.0, 25.0, 1.8),
    new(5, 70.0, 80.0, 4.2),
    new(6, 90.0, 100.0, 2.5)
];

const double speedThreshold = 2.5;

Console.WriteLine("=== Fast boids (speed >= 2.5), fastest first ===");

List<string> fastBoidDescriptions = boids
    .Where(boid => boid.Speed >= speedThreshold)
    .OrderByDescending(boid => boid.Speed)
    .Select(boid => $"Boid {boid.Id}: speed {boid.Speed:F1}")
    .ToList();

foreach (string description in fastBoidDescriptions)
{
    Console.WriteLine(description);
}

Console.WriteLine("\n=== Condition checks ===");

Console.WriteLine(
    $"Any boid faster than 4.0: {boids.Any(boid => boid.Speed > 4.0)}");

Console.WriteLine(
    $"All boids have non-negative speed: {boids.All(boid => boid.Speed >= 0)}");

Console.WriteLine("\n=== Statistics ===");

Console.WriteLine($"Total boids: {boids.Count}");

double averageSpeed = boids.Count == 0
    ? 0.0
    : boids.Average(boid => boid.Speed);

Console.WriteLine($"Average speed: {averageSpeed:F2}");

Console.WriteLine("\n=== First boid with speed >= 4.0 ===");

Boid? qualifyingBoid = boids.FirstOrDefault(boid => boid.Speed >= 4.0);

if (qualifyingBoid is not null)
{
    Console.WriteLine(
        $"Boid {qualifyingBoid.Id}: speed {qualifyingBoid.Speed:F1}");
}
else
{
    Console.WriteLine("No matching boid found.");
}

Console.WriteLine("\n=== Boids by speed category ===");

var speedGroups = boids.GroupBy(
    boid => boid.Speed >= speedThreshold ? "Fast" : "Slow");

foreach (var group in speedGroups)
{
    Console.WriteLine($"{group.Key}: {group.Count()} boid(s)");
}

Console.WriteLine("\n=== Deferred execution ===");

var fastMovingBoids = boids.Where(boid => boid.Speed >= 3.0);

boids.Add(new Boid(7, 110.0, 120.0, 3.5));

Console.WriteLine("Added Boid 7 before enumerating the query.");

foreach (Boid boid in fastMovingBoids)
{
    Console.WriteLine($"Boid {boid.Id}: speed {boid.Speed:F1}");
}

Console.WriteLine("\n=== Materialized snapshot ===");

List<Boid> fastBoidSnapshot = boids
    .Where(boid => boid.Speed >= 4.0)
    .ToList();

boids.Add(new Boid(8, 130.0, 140.0, 5.0));

Console.WriteLine("Snapshot after adding Boid 8:");

foreach (Boid boid in fastBoidSnapshot)
{
    Console.WriteLine($"Boid {boid.Id}: speed {boid.Speed:F1}");
}

Console.WriteLine("Current boids with speed >= 4.0:");

foreach (Boid boid in boids.Where(boid => boid.Speed >= 4.0))
{
    Console.WriteLine($"Boid {boid.Id}: speed {boid.Speed:F1}");
}

sealed class Boid(int id, double x, double y, double speed)
{
    public int Id { get; } = id;
    public double X { get; } = x;
    public double Y { get; } = y;
    public double Speed { get; } = speed;
}


SimulationBuffer<double> speedBuffer = new();
speedBuffer.Add(1.2);
speedBuffer.Add(2.5);
speedBuffer.Add(3.0);
speedBuffer.Add(4.2);

SimulationBuffer<string> identifierBuffer = new();
identifierBuffer.Add("Boid Alpha");
identifierBuffer.Add("Boid Beta");
identifierBuffer.Add("Boid Gamma");

Console.WriteLine("=== Boid speeds ===");
Console.WriteLine($"Count: {speedBuffer.Count}");
PrintItems(speedBuffer.Items);

Console.WriteLine("\n=== Boid identifiers ===");
Console.WriteLine($"Count: {identifierBuffer.Count}");
PrintItems(identifierBuffer.Items);

Console.WriteLine("\n=== Generic entity updates ===");

List<Boid> boids =
[
    new(1, 1.5),
    new(2, 2.0)
];

SimulationUpdater.UpdateEntities(boids, 1.0);

PrintItems(boids.Select(boid => boid.Describe()));

Console.WriteLine("\n=== Speed adjustments ===");

SpeedAdjustment accelerate = SpeedAdjustments.Accelerate;
SpeedAdjustment scale = SpeedAdjustments.Scale;

Console.WriteLine(
    $"Accelerated speed: {SpeedAdjustments.Apply(2.0, accelerate):F1}");

Console.WriteLine(
    $"Scaled speed: {SpeedAdjustments.Apply(2.0, scale):F1}");

Console.WriteLine("\n=== Lambda filtering ===");

List<double> fastSpeeds = speedBuffer.Items
    .Where(speed => speed > 2.5)
    .ToList();

PrintItems(fastSpeeds);

Console.WriteLine("\n=== Simulation update event ===");

SimulationUpdatePublisher publisher = new();

publisher.UpdateCompleted += deltaTime =>
    Console.WriteLine(
        $"Simulation update completed: {deltaTime:F2} seconds.");

publisher.CompleteUpdate(0.016);

static void PrintItems<T>(IEnumerable<T> items)
{
    foreach (T item in items)
    {
        Console.WriteLine(item);
    }
}

abstract class SimulationEntity(int id)
{
    public int Id { get; } = id;

    public abstract void Update(double deltaTime);
}

sealed class Boid(int id, double speed) : SimulationEntity(id)
{
    public double Speed { get; } = speed;

    public double DistanceTravelled { get; private set; }

    public override void Update(double deltaTime)
    {
        DistanceTravelled += Speed * deltaTime;
    }

    public string Describe()
    {
        return $"Boid {Id}: distance travelled {DistanceTravelled:F1}";
    }
}

sealed class SimulationBuffer<T>
{
    private readonly List<T> _items = [];

    public int Count => _items.Count;

    public IEnumerable<T> Items => _items;

    public void Add(T item)
    {
        _items.Add(item);
    }
}

delegate double SpeedAdjustment(double speed);

static class SpeedAdjustments
{
    public static double Accelerate(double speed)
    {
        return speed + 0.5;
    }

    public static double Scale(double speed)
    {
        return speed * 1.5;
    }

    public static double Apply(double speed, SpeedAdjustment adjustment)
    {
        return adjustment(speed);
    }
}

static class SimulationUpdater
{
    public static void UpdateEntities<T>(
        IEnumerable<T> entities,
        double deltaTime)
        where T : SimulationEntity
    {
        foreach (T entity in entities)
        {
            entity.Update(deltaTime);
        }
    }
}

sealed class SimulationUpdatePublisher
{
    public event Action<double>? UpdateCompleted;

    public void CompleteUpdate(double deltaTime)
    {
        UpdateCompleted?.Invoke(deltaTime);
    }
}

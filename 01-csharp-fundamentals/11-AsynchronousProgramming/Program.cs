
using System.Diagnostics;

using CancellationTokenSource cancellationSource = new();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationSource.Cancel();
};

CancellationToken cancellationToken = cancellationSource.Token;

try
{
    Console.WriteLine("=== Concurrent data loading ===");

    Stopwatch loadingTimer = Stopwatch.StartNew();

    Task<int> boidCountTask = LoadBoidCountAsync(cancellationToken);
    Task<double> movementSpeedTask = LoadMovementSpeedAsync(cancellationToken);

    await Task.WhenAll(boidCountTask, movementSpeedTask);

    int boidCount = await boidCountTask;
    double movementSpeed = await movementSpeedTask;

    loadingTimer.Stop();

    Console.WriteLine($"Boid count: {boidCount:N0}");
    Console.WriteLine($"Movement speed: {movementSpeed:F1}");
    Console.WriteLine(
        $"Both loads completed in {loadingTimer.Elapsed.TotalSeconds:F2} seconds.");

    Console.WriteLine("\n=== CPU-bound calculation ===");

    // Demonstration only: a trivial calculation normally does not need Task.Run.
    double averageSpeed = await Task.Run(
        () => CalculateAverageSpeed(boidCount, movementSpeed),
        cancellationToken);

    Console.WriteLine($"Average simulated boid speed: {averageSpeed:F2}");

    Console.WriteLine("\n=== Sequential versus concurrent execution ===");

    Stopwatch sequentialTimer = Stopwatch.StartNew();

    await LoadBoidCountAsync(cancellationToken);
    await LoadMovementSpeedAsync(cancellationToken);

    sequentialTimer.Stop();

    Console.WriteLine(
        $"Sequential loading: {sequentialTimer.Elapsed.TotalSeconds:F2} seconds.");

    Stopwatch concurrentTimer = Stopwatch.StartNew();

    Task<int> sequentialComparisonCountTask =
        LoadBoidCountAsync(cancellationToken);

    Task<double> sequentialComparisonSpeedTask =
        LoadMovementSpeedAsync(cancellationToken);

    await Task.WhenAll(
        sequentialComparisonCountTask,
        sequentialComparisonSpeedTask);

    concurrentTimer.Stop();

    Console.WriteLine(
        $"Concurrent loading: {concurrentTimer.Elapsed.TotalSeconds:F2} seconds.");
}
catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
{
    Console.WriteLine("\nOperation canceled.");
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Operation failed: {exception.Message}");
}

static async Task<int> LoadBoidCountAsync(
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();

    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

    return 100_000;
}

static async Task<double> LoadMovementSpeedAsync(
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();

    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

    return 2.5;
}

static double CalculateAverageSpeed(int boidCount, double baseSpeed)
{
    if (boidCount <= 0)
    {
        throw new ArgumentOutOfRangeException(nameof(boidCount));
    }

    if (!double.IsFinite(baseSpeed) || baseSpeed <= 0)
    {
        throw new ArgumentOutOfRangeException(nameof(baseSpeed));
    }

    double totalSpeed = 0;

    for (int i = 0; i < boidCount; i++)
    {
        double multiplier = 1.0 + (i % 10) * 0.01;
        totalSpeed += baseSpeed * multiplier;
    }

    return totalSpeed / boidCount;
}

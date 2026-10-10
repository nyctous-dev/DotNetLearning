
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddTransient<IMovementCalculator, MovementCalculator>();
services.AddTransient<Simulation>();
services.AddTransient<OperationTracker>();

using ServiceProvider provider = services.BuildServiceProvider(
    new ServiceProviderOptions
    {
        ValidateOnBuild = true,
        ValidateScopes = true
    });

Console.WriteLine("=== Movement calculations ===");

Simulation simulation = provider.GetRequiredService<Simulation>();

Console.WriteLine($"Speed: {simulation.CalculateSpeed(2.0, 1.5):F2}");
Console.WriteLine($"Speed: {simulation.CalculateSpeed(1.2, 2.0):F2}");
Console.WriteLine($"Speed: {simulation.CalculateSpeed(3.0, 0.75):F2}");

Console.WriteLine("\n=== Replaceable calculator ===");

var fixedServices = new ServiceCollection();

fixedServices.AddTransient<IMovementCalculator, FixedValueMovementCalculator>();
fixedServices.AddTransient<Simulation>();

using ServiceProvider fixedProvider = fixedServices.BuildServiceProvider();

Simulation fixedSimulation = fixedProvider.GetRequiredService<Simulation>();

Console.WriteLine(
    $"Fixed calculator result: {fixedSimulation.CalculateSpeed(2.0, 1.5):F2}");

Console.WriteLine("\n=== Transient lifetime ===");

OperationTracker firstTracker =
    provider.GetRequiredService<OperationTracker>();

OperationTracker secondTracker =
    provider.GetRequiredService<OperationTracker>();

Console.WriteLine($"First instance:  {firstTracker.Id}");
Console.WriteLine($"Second instance: {secondTracker.Id}");
Console.WriteLine($"Different instances: {firstTracker.Id != secondTracker.Id}");

interface IMovementCalculator
{
    double Calculate(double baseSpeed, double multiplier);
}

sealed class MovementCalculator : IMovementCalculator
{
    public double Calculate(double baseSpeed, double multiplier)
    {
        return baseSpeed * multiplier;
    }
}

sealed class FixedValueMovementCalculator : IMovementCalculator
{
    public double Calculate(double baseSpeed, double multiplier)
    {
        return 42.0;
    }
}

sealed class Simulation(IMovementCalculator calculator)
{
    public double CalculateSpeed(double baseSpeed, double multiplier)
    {
        return calculator.Calculate(baseSpeed, multiplier);
    }
}

sealed class OperationTracker
{
    public Guid Id { get; } = Guid.NewGuid();
}



// dotnet add package Microsoft.Extensions.DependencyInjection
// dotnet restore
// dotnet run
    // === Movement calculations ===
    // Speed: 3.00
    // Speed: 2.40
    // Speed: 2.25

    // === Replaceable calculator ===
    // Fixed calculator result: 42.00

    // === Transient lifetime ===
    // First instance:  c238d703-ae58-4443-a812-7dcd86affd8f
    // Second instance: 6331b7a1-1620-4f56-ba0f-3cd9b3c61bf4
    // Different instances: True







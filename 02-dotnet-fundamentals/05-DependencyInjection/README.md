# Dependency Injection

## Micro Goal

Understand dependency injection in .NET, how services are registered and resolved through the built-in dependency injection container, and how constructor injection reduces coupling and improves testability.

## Concepts Covered

- Dependencies and tight coupling
- Inversion of Control (IoC)
- Dependency Injection (DI)
- The built-in .NET service container
- Service registration and resolution
- Constructor injection
- Service lifetimes: transient, scoped, and singleton
- Interfaces and implementation classes
- `IServiceCollection` and `IServiceProvider`
- Resolving dependencies through the container
- Service lifetime rules and captive dependencies
- Disposal of services
- Testing classes with injected dependencies

## Completion Criteria

- [ ] Explain what a dependency is and why injecting it is preferable to constructing it directly inside a class.
- [ ] Define an interface and an implementation class.
- [ ] Register services with `IServiceCollection`.
- [ ] Build a service provider and resolve a service.
- [ ] Use constructor injection to provide a dependency to a class.
- [ ] Explain the differences between transient, scoped, and singleton lifetimes.
- [ ] Demonstrate how service lifetimes affect object identity.
- [ ] Avoid injecting scoped services into singleton services.
- [ ] Explain how dependency injection supports unit testing.
- [ ] Complete the challenge with a small, working console application.

## Lecture

### 1. What is a dependency?

A dependency is an object or service that another part of a program needs to perform its work.

For example, a Boids simulation may need a service that calculates movement, a service that logs events, or a service that provides simulation settings.

A class can construct its own dependencies:

```csharp
class Simulation
{
    private readonly MovementCalculator _calculator = new();

    public double CalculateSpeed(double baseSpeed, double multiplier)
    {
        return _calculator.Calculate(baseSpeed, multiplier);
    }
}
```

This works, but `Simulation` is directly tied to the `MovementCalculator` implementation. If you want to replace that calculator with a different implementation or a test double, you must change the `Simulation` class.

The class is responsible both for its own behavior and for deciding how its dependency is created.

Dependency injection separates those responsibilities. Instead of constructing its dependency, a class receives the dependency from outside.

### 2. Dependency Injection and Inversion of Control

**Dependency Injection (DI)** is a technique in which an object receives the dependencies it needs rather than creating them itself.

**Inversion of Control (IoC)** is the broader principle of transferring control over some aspect of a program to another component or framework. A DI container is one way to apply IoC: the container manages service construction and supplies dependencies.

The same example can be rewritten to receive a calculator:

```csharp
class Simulation
{
    private readonly MovementCalculator _calculator;

    public Simulation(MovementCalculator calculator)
    {
        _calculator = calculator;
    }

    public double CalculateSpeed(double baseSpeed, double multiplier)
    {
        return _calculator.Calculate(baseSpeed, multiplier);
    }
}
```

The constructor declares the dependency. The caller supplies it:

```csharp
MovementCalculator calculator = new();
Simulation simulation = new(calculator);

Console.WriteLine(simulation.CalculateSpeed(2.0, 1.5));
```

`Simulation` no longer constructs the calculator itself. This is constructor injection, one of the most common forms of DI in .NET.

The example still depends on the concrete `MovementCalculator` type. Interfaces can reduce that coupling further.

### 3. Interfaces and implementation classes

An interface defines a contract: the operations a type must provide, without specifying the implementation of those operations.

```csharp
interface IMovementCalculator
{
    double Calculate(double baseSpeed, double multiplier);
}
```

An implementation provides the actual behavior:

```csharp
class MovementCalculator : IMovementCalculator
{
    public double Calculate(double baseSpeed, double multiplier)
    {
        return baseSpeed * multiplier;
    }
}
```

The simulation can now depend on the interface rather than the concrete implementation:

```csharp
class Simulation
{
    private readonly IMovementCalculator _calculator;

    public Simulation(IMovementCalculator calculator)
    {
        _calculator = calculator;
    }

    public double CalculateSpeed(double baseSpeed, double multiplier)
    {
        return _calculator.Calculate(baseSpeed, multiplier);
    }
}
```

This design separates the contract from the implementation. Any class that implements `IMovementCalculator` can be supplied to `Simulation`.

For example, a test could provide a substitute implementation that returns a predictable value. The simulation does not need to know which implementation it receives.

Interfaces are useful when a dependency has multiple implementations, needs to be replaced during testing, or represents a boundary between parts of the application. They should not be added automatically to every class; use them where they provide a meaningful separation.

### 4. The built-in .NET service container

.NET provides a built-in dependency injection container. It manages service registrations, constructs requested services, and supplies their constructor dependencies.

The main types used in a simple application are:

- `IServiceCollection`: the collection of service registrations.
- `ServiceCollection`: a concrete implementation used to configure registrations.
- `IServiceProvider`: the built container used to resolve services.

These types are available through the `Microsoft.Extensions.DependencyInjection` package. A standard ASP.NET Core application includes dependency injection support by default. A standalone console project may need an explicit package reference.

From the directory containing your console project, add the package with:

```bash
dotnet add package Microsoft.Extensions.DependencyInjection
```

Then register the calculator and simulation:

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddTransient<IMovementCalculator, MovementCalculator>();
services.AddTransient<Simulation>();

using ServiceProvider provider = services.BuildServiceProvider();

Simulation simulation = provider.GetRequiredService<Simulation>();

Console.WriteLine(simulation.CalculateSpeed(2.0, 1.5));
```

The registrations tell the container how to provide each service:

```csharp
services.AddTransient<IMovementCalculator, MovementCalculator>();
services.AddTransient<Simulation>();
```

When `Simulation` is requested, the container sees that its constructor requires an `IMovementCalculator`. It finds the corresponding registration, creates a `MovementCalculator`, and passes it to the constructor.

`BuildServiceProvider()` creates the provider from the registrations. `GetRequiredService<T>()` requests a service and throws an exception if the requested service is not registered or cannot be constructed.

The container is doing the object construction and wiring, but the class still declares its dependencies explicitly through its constructor.

For this small console example, building the provider directly is appropriate. In ASP.NET Core applications, the host normally configures and manages the service provider, so you generally register services with the application's service collection rather than building a second provider manually.

### 5. Registering services

The container supports several common registration methods:

```csharp
services.AddTransient<IMovementCalculator, MovementCalculator>();
services.AddScoped<Simulation>();
services.AddSingleton<SimulationSettings>();
```

The registration methods determine the service lifetime. They do not change the interface contract or the behavior of the implementation.

You can register a concrete type, an interface and implementation pair, or a preconstructed instance.

For example, a simple settings object could be registered as a singleton instance:

```csharp
SimulationSettings settings = new()
{
    MaximumBoids = 500
};

services.AddSingleton(settings);
```

The container returns that registered instance when `SimulationSettings` is requested. Registering an existing instance means you are responsible for understanding its ownership and disposal requirements.

For interface-based services, the interface and implementation types must be compatible. A class registered as `IMovementCalculator` must implement that interface.

### 6. Service lifetimes

A service lifetime determines how long the container retains a service instance and when it creates another one.

.NET's three standard lifetimes are transient, scoped, and singleton.

**Transient — `AddTransient`**

A new instance is created each time the service is resolved from the container.

```csharp
services.AddTransient<OperationTracker>();
```

Transient services are useful for lightweight, stateless operations where sharing an instance is unnecessary.

If a transient service is injected into a longer-lived service, that dependency may remain referenced for the lifetime of the receiving service. Transient does not mean that an instance is guaranteed to be discarded immediately after one method call.

**Scoped — `AddScoped`**

One instance is created per dependency injection scope.

```csharp
services.AddScoped<SimulationSession>();
```

Within the same scope, resolving `SimulationSession` repeatedly returns the same instance. A different scope receives a different instance.

ASP.NET Core normally creates a scope for each HTTP request. A standalone console application does not automatically create request scopes, so you must create a scope explicitly when you need scoped behavior.

**Singleton — `AddSingleton`**

One instance is created and shared for the lifetime of the service provider.

```csharp
services.AddSingleton<SimulationSettings>();
```

Singletons are useful for shared services or immutable settings. If a singleton holds mutable state and multiple operations access it concurrently, you must account for thread safety.

The practical distinction is:

| Lifetime | Instance behavior | Typical use |
|---|---|---|
| Transient | New instance per resolution | Lightweight operations |
| Scoped | One instance per scope | Per-request or per-operation state |
| Singleton | Shared for the provider's lifetime | Shared services and stable state |

The right lifetime depends on the service's state, dependencies, and intended ownership. Do not select a lifetime solely because one option seems faster or simpler.

### 7. Observing service lifetimes

You can observe lifetimes by registering a service that receives a unique identifier when constructed:

```csharp
class OperationTracker
{
    public Guid Id { get; } = Guid.NewGuid();
}
```

Register it with different lifetimes and compare the identifiers:

```csharp
services.AddTransient<OperationTracker>();
services.AddScoped<OperationTracker>();
services.AddSingleton<OperationTracker>();
```

Then resolve each service more than once inside a scope. Transient resolutions should produce different identifiers, while scoped and singleton resolutions should retain the same identifiers within their respective lifetimes.

For a scoped service, create a second scope and resolve it again. Its identifier should differ from the one created in the first scope.

This is a useful experiment because the lifetime is observable behavior, not just a label attached to a registration.

### 8. Scopes and correct lifetime usage

A scope represents a boundary for services registered with the scoped lifetime. In a console application, create a scope using `CreateScope()`:

```csharp
using IServiceScope scope = provider.CreateScope();

SimulationSession session =
    scope.ServiceProvider.GetRequiredService<SimulationSession>();
```

Disposing the scope disposes scoped services that the container owns and that implement the appropriate disposal interfaces.

A common lifetime mistake is injecting a scoped service into a singleton. The singleton may retain the scoped instance long after the scope should have ended. This is often called a captive dependency.

For example, a singleton service should not directly retain a request-scoped session. Instead, redesign the dependency relationship or create and use an explicit scope at the appropriate boundary.

A transient dependency injected into a singleton can also effectively live as long as that singleton because the singleton retains it. The registration's declared lifetime does not override the lifetime of references held by other objects.

When using the standard .NET hosting setup, enable scope validation in development where possible. It can help detect invalid lifetime relationships rather than allowing them to become subtle runtime bugs.

### 9. Constructor injection and required dependencies

Constructor injection makes a class's required dependencies visible:

```csharp
class Simulation
{
    private readonly IMovementCalculator _calculator;

    public Simulation(IMovementCalculator calculator)
    {
        _calculator = calculator
            ?? throw new ArgumentNullException(nameof(calculator));
    }
}
```

The `readonly` field is assigned during construction and cannot later be reassigned by ordinary instance code. This is appropriate for a dependency that should remain fixed for the object's lifetime.

Avoid making required dependencies optional merely to simplify construction. If a class cannot work correctly without a dependency, require it through the constructor.

Also avoid resolving dependencies manually throughout application logic using `IServiceProvider`. This is sometimes called the service locator pattern. It hides a class's requirements and makes its behavior harder to understand and test. Prefer constructor injection for normal application services.

### 10. Dependency injection and unit testing

Dependency injection allows a class to be tested with a controlled implementation of its dependency.

Suppose the calculator's contract is:

```csharp
interface IMovementCalculator
{
    double Calculate(double baseSpeed, double multiplier);
}
```

A test implementation can return a predictable value:

```csharp
class FixedMovementCalculator : IMovementCalculator
{
    public double Calculate(double baseSpeed, double multiplier)
    {
        return 42.0;
    }
}
```

The simulation can receive that implementation without changing its own code:

```csharp
IMovementCalculator calculator = new FixedMovementCalculator();
Simulation simulation = new Simulation(calculator);

Console.WriteLine(simulation.CalculateSpeed(2.0, 1.5)); // 42
```

This example demonstrates substitutability, but it is not yet a formal unit test. A proper test project would use a test framework and assertions to verify expected behavior automatically.

The key principle is that the simulation depends on the contract, not the particular calculator. A test can provide a fake, stub, or mock implementation that isolates the behavior being tested.

Dependency injection does not automatically make code testable. Good boundaries, clear contracts, and limited responsibilities are still necessary.

### 11. Disposal and service ownership

Some services use resources that must be released, such as streams, database connections, or other disposable objects. .NET commonly expresses this requirement through `IDisposable` or `IAsyncDisposable`.

When the container creates and owns a disposable service, it normally disposes that service when its lifetime ends: transient services when the owning provider is disposed, scoped services when their scope is disposed, and singleton services when the root provider is disposed.

A service resolved from the container should generally not be manually disposed while the container still owns it. Doing so can leave the container holding a reference to an already-disposed object.

For a console application that creates its own provider, dispose the provider when finished:

```csharp
using ServiceProvider provider = services.BuildServiceProvider();
```

For scoped services, dispose the scope. In a hosted ASP.NET Core application, the host manages the root provider and application scopes.

The main rule is to understand who owns an object and which component is responsible for disposing it.

## Challenge

Build a console application that uses dependency injection to calculate and report movement speeds for a Boids simulation.

Your program must satisfy all of the following requirements:

1. **Define an interface:** Create `IMovementCalculator` with a method that calculates movement speed from a base speed and multiplier.
2. **Implement the interface:** Create `MovementCalculator` that returns the base speed multiplied by the multiplier.
3. **Create a simulation service:** Create `Simulation` that receives `IMovementCalculator` through constructor injection and uses it to calculate movement speed.
4. **Register services:** Use `ServiceCollection` to register the calculator and simulation with appropriate lifetimes.
5. **Resolve the simulation:** Build the provider and resolve `Simulation` through `GetRequiredService`.
6. **Demonstrate calculation:** Calculate and print movement speeds for at least three different combinations of base speed and multiplier.
7. **Demonstrate a replaceable dependency:** Create a second `IMovementCalculator` implementation that returns a fixed value. Show that a `Simulation` receiving this implementation uses its result without changing the `Simulation` class.
8. **Demonstrate a service lifetime:** Create a simple service with a unique identifier and register it as transient. Resolve it twice and print the identifiers to demonstrate that the instances differ.
9. **Manage ownership:** Dispose of the service provider correctly.
10. **Keep responsibilities clear:** Put calculation behavior in the calculator, coordination in the simulation service, and registrations and demonstration code in the top-level statements.

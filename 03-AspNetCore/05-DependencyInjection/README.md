# Dependency Injection in Web Apps

## Micro Goal

Understand how ASP.NET Core uses dependency injection to provide application services to HTTP endpoints, separate request handling from business logic, and manage service lifetimes correctly across requests.

## Concepts Covered

- Dependency injection in ASP.NET Core
- The built-in dependency injection container
- Registering services in `Program.cs`
- Injecting services into Minimal API handlers
- Constructor injection in application services
- Service lifetimes in web applications
- Scoped services and HTTP request scope
- Injecting multiple dependencies
- Separating endpoints from business logic
- Service registration and resolution errors
- Dependency injection in integration tests
- Avoiding service locator patterns and unnecessary abstractions

## Completion Criteria

- [ ] Register an application service in the ASP.NET Core dependency injection container.
- [ ] Inject a service into a Minimal API endpoint handler.
- [ ] Move business logic into a separate service rather than implementing it entirely inside an endpoint.
- [ ] Use constructor injection to provide one service with its dependencies.
- [ ] Explain how transient, scoped, and singleton lifetimes behave in a web application.
- [ ] Demonstrate that a scoped service is shared within a request but normally recreated for a separate request.
- [ ] Explain why a singleton must not directly depend on a scoped service.
- [ ] Recognize common service registration and resolution errors.
- [ ] Test an endpoint that uses a registered service.
- [ ] Complete the challenge with a small, working ASP.NET Core application.

## Lecture

### 1. Dependency injection in ASP.NET Core

Dependency injection (DI) is a technique for providing an object with the services it needs instead of having the object create those dependencies itself.

For example, an HTTP endpoint might need a service that calculates a boid's movement speed. The endpoint should not need to construct that service manually every time it handles a request. Instead, ASP.NET Core can resolve the service from its dependency injection container and supply it to the endpoint handler.

ASP.NET Core includes a built-in dependency injection container. Services are registered during application startup, and the framework resolves them when they are required.

A simplified request flow looks like this:

```text
HTTP request
    |
    v
ASP.NET Core endpoint handler
    |
    v
Injected application service
    |
    v
Result returned as HTTP response
```

The endpoint handles HTTP concerns, such as receiving parameters and returning a response. The service performs the application logic. This separation makes the logic easier to maintain and test.

Dependency injection does not require an external library for ordinary ASP.NET Core applications.

### 2. Registering a service in `Program.cs`

Consider an application service that calculates movement speed.

```csharp
public sealed class SpeedCalculator
{
    public double Calculate(double baseSpeed, double multiplier)
    {
        return baseSpeed * multiplier;
    }
}
```

In an ASP.NET Core Minimal API application, register the service before building the application:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SpeedCalculator>();

var app = builder.Build();

app.Run();
```

The call to `AddSingleton` registers `SpeedCalculator` with the DI container. The container can then provide an instance when a registered dependency is requested.

The three commonly used service lifetimes are:

- `AddTransient<TService>()`: creates a new instance each time the service is resolved.
- `AddScoped<TService>()`: creates one instance per dependency injection scope. In typical ASP.NET Core HTTP request processing, that means one instance per request.
- `AddSingleton<TService>()`: uses one instance for the lifetime of the application service provider.

These lifetimes determine how instances are reused; they do not determine what the service does.

For a stateless calculator with no mutable per-request data, a singleton is a reasonable choice. A service that stores information for one HTTP request is usually scoped. A lightweight service that should not share instance state between resolutions may be transient.

Choose the lifetime based on the service's dependencies and state, not simply on which option creates fewer objects.

### 3. Injecting a service into a Minimal API endpoint

Once a service is registered, ASP.NET Core can supply it directly to a Minimal API handler.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SpeedCalculator>();

var app = builder.Build();

app.MapGet(
    "/speed",
    (double baseSpeed, double multiplier, SpeedCalculator calculator) =>
    {
        double result = calculator.Calculate(baseSpeed, multiplier);

        return Results.Ok(new { baseSpeed, multiplier, result });
    });

app.Run();
```

The `SpeedCalculator` parameter is resolved from DI because its type is registered as a service. The numeric parameters are bound from the HTTP request's query string.

For example, a request to:

```text
GET /speed?baseSpeed=2&multiplier=1.5
```

returns a successful HTTP response containing JSON similar to:

```json
{
  "baseSpeed": 2,
  "multiplier": 1.5,
  "result": 3
}
```

The anonymous object is serialized into JSON by ASP.NET Core's HTTP result infrastructure.

Notice the distinction between parameter sources:

- `baseSpeed` and `multiplier` are request data.
- `calculator` is an application service supplied through dependency injection.

ASP.NET Core uses the endpoint's parameter types and metadata to determine how parameters are bound. For more complex applications, explicit request DTOs can make the HTTP contract clearer.

### 4. Separate endpoint handling from business logic

An endpoint should generally coordinate HTTP input, invoke application logic, and translate the result into an HTTP response. It should not contain all the application's business rules.

For example, the endpoint should not need to duplicate the speed calculation in several routes. A separate service centralizes that logic.

```csharp
public sealed class SpeedCalculator
{
    public double Calculate(double baseSpeed, double multiplier)
    {
        if (!double.IsFinite(baseSpeed) || baseSpeed < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseSpeed));
        }

        if (!double.IsFinite(multiplier) || multiplier < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(multiplier));
        }

        return baseSpeed * multiplier;
    }
}
```

This service owns the calculation and its input rules. Multiple endpoints can reuse it, and it can be tested independently of HTTP.

The endpoint still needs an appropriate strategy for invalid input and exceptions. Throwing an exception from a service does not automatically guarantee that the client receives a particular status code or a useful error response. HTTP error handling is a separate responsibility.

The key benefit of DI is that the endpoint does not need to know how the service is constructed. It only needs a service of the required type.

### 5. Constructor injection in application services

Services can depend on other services. ASP.NET Core can resolve those dependencies recursively when constructing an object.

Suppose movement calculations need a speed calculator:

```csharp
public sealed class SpeedCalculator
{
    public double Calculate(double baseSpeed, double multiplier)
    {
        return baseSpeed * multiplier;
    }
}

public sealed class MovementService
{
    private readonly SpeedCalculator _speedCalculator;

    public MovementService(SpeedCalculator speedCalculator)
    {
        _speedCalculator = speedCalculator;
    }

    public double CalculateMovementSpeed(
        double baseSpeed,
        double multiplier)
    {
        return _speedCalculator.Calculate(baseSpeed, multiplier);
    }
}
```

Register both services:

```csharp
builder.Services.AddSingleton<SpeedCalculator>();
builder.Services.AddSingleton<MovementService>();
```

Now an endpoint can request `MovementService` directly:

```csharp
app.MapGet(
    "/movement-speed",
    (double baseSpeed, double multiplier, MovementService movement) =>
        Results.Ok(new
        {
            speed = movement.CalculateMovementSpeed(
                baseSpeed,
                multiplier)
        }));
```

When ASP.NET Core resolves `MovementService`, it also resolves the required `SpeedCalculator` and passes it to the constructor.

This is **constructor injection**. It makes required dependencies explicit and allows a service to retain the dependencies it needs.

The private readonly field prevents the reference from being reassigned after construction. It does not make the referenced object immutable.

Constructor injection also makes dependencies easier to replace in tests. A service can be tested with a real dependency, or with a test double when a dependency's behavior needs to be controlled.

### 6. Service lifetimes in web applications

Service lifetimes become particularly important in a web application because many requests may be processed over the application's lifetime, and some requests may be processed concurrently.

**Transient**

A transient service is created each time it is resolved from the container.

```csharp
builder.Services.AddTransient<SpeedCalculator>();
```

Transient services are suitable for lightweight operations that do not need shared instance state. However, transient does not mean that an instance is recreated every time a method is called. Once a transient object has been injected into another service, that consumer holds its reference.

**Scoped**

A scoped service is created once per scope. During typical HTTP request processing, ASP.NET Core creates a request scope, so repeated resolutions of that service within the same request generally return the same instance.

```csharp
builder.Services.AddScoped<SimulationRequestContext>();
```

A scoped service is appropriate for state that should be shared within one request but not carried into a different request. Database contexts, such as Entity Framework Core's `DbContext`, are commonly registered as scoped.

A scoped service should not be used to store user-specific request data in a singleton.

**Singleton**

A singleton service is created once for the lifetime of the service provider and shared across requests.

```csharp
builder.Services.AddSingleton<SpeedCalculator>();
```

Singleton services can be appropriate for stateless calculations or shared, thread-safe components. Because requests can execute concurrently, a singleton's mutable state must be designed for concurrent access.

A singleton is not automatically faster or better than a scoped or transient service. Its lifetime must match its responsibilities and dependencies.

### 7. Understanding request scope

Consider a scoped service that assigns an identifier when it is constructed:

```csharp
public sealed class RequestMarker
{
    public Guid Id { get; } = Guid.NewGuid();
}
```

Register it as scoped:

```csharp
builder.Services.AddScoped<RequestMarker>();
```

Two endpoints can return the identifier:

```csharp
app.MapGet("/marker/first", (RequestMarker marker) =>
    Results.Ok(new { marker.Id }));

app.MapGet("/marker/second", (RequestMarker marker) =>
    Results.Ok(new { marker.Id }));
```

Within each request, the injected marker belongs to that request's scope. Calling `/marker/first` and then `/marker/second` normally produces different identifiers because those are separate HTTP requests.

To demonstrate reuse within one request, resolve the service twice within a single handler:

```csharp
app.MapGet("/marker/compare", (
    RequestMarker first,
    RequestMarker second) =>
{
    return Results.Ok(new
    {
        FirstId = first.Id,
        SecondId = second.Id,
        SameInstance = ReferenceEquals(first, second)
    });
});
```

Because both parameters are the same scoped service type resolved within the same request scope, they normally refer to the same instance. `SameInstance` should therefore be `true`.

This illustrates why scope is important: scoped does not mean one instance per endpoint or one instance per user. It means one instance per DI scope.

### 8. Avoiding invalid lifetime dependencies

A singleton lives for the lifetime of the application, while a scoped service normally lives for one request. Injecting a scoped service directly into a singleton can cause the scoped instance to be retained beyond its intended lifetime.

For example, this registration is problematic if `RequestMarker` is scoped:

```csharp
builder.Services.AddScoped<RequestMarker>();
builder.Services.AddSingleton<SomeSingletonService>();
```

If `SomeSingletonService` has a constructor parameter of type `RequestMarker`, it tries to retain a scoped dependency for the singleton's lifetime.

With scope validation enabled, ASP.NET Core can detect and reject this captive dependency. Without proper validation, the scoped object may effectively behave as though it were shared far beyond a single request.

The normal solution is to align the lifetimes or redesign the dependency relationship. Do not change a scoped service to singleton merely to silence a lifetime error.

When a singleton genuinely needs to create short-lived work in a new scope, `IServiceScopeFactory` can be used to create and dispose a scope explicitly. This is a more advanced pattern and should only be introduced when the lifetime requirement justifies it.

### 9. Service registration and resolution errors

A dependency must be registered, or otherwise provided by the framework, before the container can resolve it.

For example, if an endpoint requires `MovementService` but the application never registers it, the application can fail when the endpoint is invoked. The exception commonly indicates that the service type could not be resolved.

A missing dependency in a constructor can cause a similar failure when the container attempts to create that service.

Common causes include:

- Forgetting to register an application service.
- Registering an implementation under an interface but requesting a different service type.
- A constructor requiring another service that is not registered.
- Creating an invalid dependency cycle.
- Using an incompatible service lifetime.

Register an implementation against its interface when the endpoint should depend on the abstraction:

```csharp
builder.Services.AddScoped<IMovementService, MovementService>();
```

Then request `IMovementService` rather than the concrete class:

```csharp
app.MapGet(
    "/movement-speed",
    (double baseSpeed, double multiplier, IMovementService movement) =>
        Results.Ok(new
        {
            speed = movement.CalculateMovementSpeed(
                baseSpeed,
                multiplier)
        }));
```

The interface and implementation must exist in the application code, and the implementation must satisfy the interface. An interface registration does not automatically register every related concrete type under every service type.

For a small learning application, injecting a concrete service is acceptable. Interfaces become useful when they provide a meaningful boundary, such as allowing multiple implementations or isolating application logic from external infrastructure.

### 10. Avoiding the service locator pattern

ASP.NET Core exposes `IServiceProvider`, which can resolve services dynamically. However, application code should generally not request `IServiceProvider` and manually locate its own dependencies when ordinary constructor or handler injection would work.

For example, this hides the actual dependency:

```csharp
app.MapGet("/speed", (IServiceProvider services) =>
{
    var calculator = services.GetRequiredService<SpeedCalculator>();

    return Results.Ok(calculator.Calculate(2.0, 1.5));
});
```

The handler's signature suggests it only needs `IServiceProvider`, even though it actually requires `SpeedCalculator`.

Prefer explicit injection:

```csharp
app.MapGet("/speed", (SpeedCalculator calculator) =>
    Results.Ok(calculator.Calculate(2.0, 1.5)));
```

The second version communicates the dependency directly and lets the framework resolve it. Explicit dependencies are easier to understand, test, and refactor.

There are legitimate cases for resolving services dynamically, such as infrastructure code that intentionally creates scopes. Those cases do not justify using the service locator pattern throughout ordinary endpoint and business logic.

### 11. Testing services and endpoints

DI makes application logic easier to test because the code that uses a service does not need to construct that service itself.

A simple unit test can test `SpeedCalculator` directly without starting a web server:

```csharp
[Fact]
public void Calculate_ReturnsProductOfInputs()
{
    var calculator = new SpeedCalculator();

    double result = calculator.Calculate(2.0, 1.5);

    Assert.Equal(3.0, result);
}
```

This verifies the calculation, but it does not verify that ASP.NET Core registered the service or supplied it correctly to an endpoint.

For endpoint behavior, an integration test can start the application using the ASP.NET Core test host, send an HTTP request, and inspect the response. The test can also replace service registrations when it needs to control application behavior.

Unit tests and integration tests serve different purposes:

- Unit tests verify focused application logic in isolation.
- Integration tests verify that components work together, including routing, DI registration, request binding, and HTTP responses.

A useful test strategy includes both when the application needs both kinds of confidence.

## Challenge

Build a small ASP.NET Core Minimal API that uses dependency injection to provide a service for calculating Boid movement speed.

Your application must satisfy all of the following requirements:

1. **Create a service:** Implement a `MovementService` with a method that calculates movement speed from a base speed and a multiplier.
2. **Validate inputs:** Reject negative or non-finite speed values with an appropriate exception.
3. **Register the service:** Register `MovementService` in the DI container using an appropriate lifetime.
4. **Create an endpoint:** Implement `GET /movement-speed` with `baseSpeed` and `multiplier` query parameters. Inject `MovementService` into the handler and return the calculated speed as JSON.
5. **Separate responsibilities:** Keep the calculation in `MovementService`. The endpoint should handle HTTP input and response creation rather than duplicating the calculation.
6. **Add a second service:** Implement a small service that provides a simulation status message. Register it and inject it into `GET /status`.
7. **Demonstrate scoped behavior:** Register a simple marker service as scoped and create an endpoint that resolves the same scoped service twice in one request. Return both identifiers and whether the references are identical.
8. **Test the calculation:** Write a unit test that verifies a known speed calculation and at least one invalid-input case.
9. **Run the application:** Start the web app and send requests to each endpoint. Verify that the responses contain the expected data.
10. **Keep the design simple:** Use clear names, appropriate service lifetimes, and explicit dependencies. Do not introduce interfaces, factories, or additional abstractions unless they serve a specific requirement.

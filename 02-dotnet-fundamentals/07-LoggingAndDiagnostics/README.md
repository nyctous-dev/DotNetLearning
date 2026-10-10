# Logging and Diagnostics

## Micro Goal

Understand how to record meaningful application events using .NET logging, configure log levels and providers, include structured data in log messages, and use diagnostic techniques to investigate unexpected behavior.

## Concepts Covered

- The `Microsoft.Extensions.Logging` abstractions
- `ILogger<T>` and dependency injection
- Log levels: `Trace`, `Debug`, `Information`, `Warning`, `Error`, and `Critical`
- Structured logging and message templates
- Logging exceptions
- Filtering logs by category and severity
- Console logging and logging providers
- Configuration-based logging with `appsettings.json`
- Logging scopes and contextual information
- Basic diagnostics and troubleshooting
- Logging best practices

## Completion Criteria

- [ ] Explain the purpose of logging and how it differs from debugging.
- [ ] Create and configure a logger using `ILogger<T>`.
- [ ] Use the appropriate log level for different situations.
- [ ] Write structured log messages with named placeholders.
- [ ] Log exceptions with their exception details.
- [ ] Configure minimum log levels in `appsettings.json`.
- [ ] Use logging scopes to associate related events with shared context.
- [ ] Diagnose a failure using log output.
- [ ] Avoid logging sensitive information and unnecessary message noise.

## Lecture

### 1. Why logging matters

Applications need a way to report what they are doing, what has gone wrong, and what information may help explain unexpected behavior. Logging records these events so that developers and operators can investigate an application without having to reproduce every issue interactively.

For example, a Boids simulation might need to record when it starts, how many boids it initializes, whether a configuration value is invalid, or why a simulation update failed.

Logging is different from debugging:

- **Logging** records selected events while an application runs. The records can be inspected afterward.
- **Debugging** involves investigating program execution using tools such as breakpoints, variable inspection, and step-by-step execution.
- **Diagnostics** is the broader process of gathering and analyzing information to understand an application's behavior, performance, and failures.

`Console.WriteLine` is useful for simple output and early exercises. However, it does not provide standardized severity levels, configurable filtering, structured message properties, or a common interface for multiple output destinations.

.NET provides logging abstractions that let application code report events without being tightly coupled to a specific logging destination.

### 2. The .NET logging abstractions

The core logging interfaces are provided by `Microsoft.Extensions.Logging`. The central interface is `ILogger`, with `ILogger<TCategoryName>` commonly used to associate logs with a class or category.

A logger is typically supplied through dependency injection rather than constructed directly inside every class.

For example, a class responsible for managing a simulation might accept a logger through its constructor:

```csharp
using Microsoft.Extensions.Logging;

public sealed class SimulationService
{
    private readonly ILogger<SimulationService> _logger;

    public SimulationService(ILogger<SimulationService> logger)
    {
        _logger = logger;
    }

    public void Start(int boidCount)
    {
        _logger.LogInformation(
            "Starting simulation with {BoidCount} boids.",
            boidCount);
    }
}
```

The logger is stored in a private readonly field so the service can use it throughout its lifetime. `ILogger<SimulationService>` identifies the category using the service's type, which makes it easier to filter and identify log entries.

This example defines a service but does not create or run it. A dependency injection container must register the required services and provide the logger when the `SimulationService` is constructed.

The `Microsoft.Extensions.Logging` abstractions are used throughout .NET, including ASP.NET Core and Blazor applications. Learning them now will make logging familiar when the project moves from console applications to web applications.

### 3. Log levels

Each log event has a severity level. The standard levels, from least severe to most severe, are:

| Level | Purpose |
|---|---|
| `Trace` | Very detailed information used to trace execution paths. |
| `Debug` | Information useful during development and investigation. |
| `Information` | Normal application events worth recording. |
| `Warning` | An unexpected or potentially problematic condition that does not necessarily stop the operation. |
| `Error` | A failure that prevents a particular operation from completing successfully. |
| `Critical` | A severe failure that may require immediate attention. |
| `None` | Used as a filtering threshold to disable logging. It is not a normal event level to emit. |

Use levels to communicate the significance of an event, not merely how frequently it occurs.

For example:

```csharp
_logger.LogTrace("Beginning a simulation update.");
_logger.LogDebug("Processing {BoidCount} boids.", boidCount);
_logger.LogInformation("Simulation started.");
_logger.LogWarning("The requested boid count is unusually high.");
_logger.LogError("The simulation update failed.");
_logger.LogCritical("The simulation service cannot continue.");
```

These calls demonstrate the available levels. In a real application, only emit events that provide useful information at the corresponding severity.

A warning does not necessarily mean an exception occurred. For example, a user entering an unsupported display mode might produce a warning while the application continues normally. An error often indicates that an operation failed. A critical event indicates a particularly severe condition.

### 4. Structured logging

A major advantage of the .NET logging abstractions is structured logging.

Consider this message:

```csharp
_logger.LogInformation(
    $"Simulation started with {boidCount} boids.");
```

String interpolation produces a completed string before the logging call receives it. The logging system gets the rendered message, but it does not receive `BoidCount` as a named property from the message template.

Prefer this form:

```csharp
_logger.LogInformation(
    "Simulation started with {BoidCount} boids.",
    boidCount);
```

The placeholder `{BoidCount}` identifies a structured property. Logging providers can retain this property separately from the rendered message, allowing log events to be searched or filtered by its value.

Another example:

```csharp
_logger.LogWarning(
    "Boid {BoidId} reached speed {Speed}.",
    boidId,
    speed);
```

The logger associates `BoidId` with `boidId` and `Speed` with `speed`. This makes the event more useful to tools that support structured log data.

Message-template placeholders should correspond to the supplied values and should use meaningful names. Avoid constructing elaborate strings before calling the logger when a structured template can express the same information.

For repeated events in a frequently executed loop, structured logging can also make it easier to aggregate events without treating every distinct rendered message as a separate message pattern.

### 5. Logging exceptions

When an operation fails with an exception, include the exception object in the log call so the logging provider can record its details.

```csharp
try
{
    LoadSimulationConfiguration();
}
catch (Exception exception)
{
    _logger.LogError(
        exception,
        "Failed to load simulation configuration.");

    throw;
}
```

The first argument after the log method is the exception, followed by the message template and any template values.

The `throw;` statement rethrows the current exception while preserving its original stack trace. This example logs the failure and allows the caller to handle it or let the application fail.

Do not catch an exception only to log it and then silently continue if the application cannot safely proceed. Also avoid logging the same exception at every layer without adding useful context, as this can create duplicate and noisy records.

If a service can handle an expected failure and continue, it may log the issue and return an appropriate result instead. The decision to recover, return a failure, or rethrow should be based on the operation's contract and the application's requirements.

Avoid logging secrets, passwords, access tokens, or sensitive personal data in exception messages or surrounding context.

### 6. Logging categories and filtering

A logging category identifies where a log event originated. With `ILogger<T>`, the category is normally based on the fully qualified name of `T`.

For example, logs emitted by `SimulationService` and `ConfigurationService` can be filtered independently by category. This helps when one part of an application produces too much detail or when a specific subsystem needs more diagnostic information.

A minimum log level acts as a threshold. If the minimum level is `Information`, `Trace` and `Debug` events are filtered out, while `Information`, `Warning`, `Error`, and `Critical` events are eligible to be emitted.

Eligibility does not guarantee that an event appears in a particular destination. The configured provider must also accept and output the event.

Filtering is important for performance and clarity. A simulation may execute many updates per second, so logging every calculation at `Information` level could produce an enormous amount of output. Detailed events may be more appropriate at `Debug` or `Trace` level and enabled only when needed.

### 7. Configuring logging with `appsettings.json`

.NET applications often use JSON configuration files to control logging behavior without changing source code.

A typical configuration section looks like this:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

This configuration establishes three rules:

- `Default` sets the minimum level for categories that do not have a more specific rule.
- `Microsoft` sets the minimum level for categories beginning with that prefix.
- `Microsoft.Hosting.Lifetime` overrides the broader `Microsoft` rule for that more specific category.

More specific category rules take precedence over broader matching rules. For categories that do not match an explicit rule, the default applies.

The configuration file alone does not guarantee that these settings are loaded. The application needs a configuration system and a logging provider that uses those settings. The default templates for ASP.NET Core applications commonly set up this infrastructure. A plain console application may need additional setup.

For a console application using the generic host, the `Microsoft.Extensions.Hosting` package provides hosting and configuration infrastructure, including integration with logging. The exact package references and setup depend on the project template and target framework.

When you reach dependency injection and configuration in this learning path, you will build that setup explicitly. For now, understand what the configuration controls and why log filtering should be configurable.

### 8. Logging providers and destinations

The logging abstractions define how application code writes events, but providers determine where those events go.

Common destinations include:

- The console, which is useful during local development.
- Debug output, which is useful when working with development tools.
- Files, when an appropriate file logging provider is configured.
- Centralized logging systems, which collect and search events from deployed applications.

The `Microsoft.Extensions.Logging.Console` package provides a console logging provider. Other destinations may require separate packages or application-specific configuration.

Application code should generally depend on `ILogger<T>` rather than a particular provider. This separation means that a service can continue using the same logging calls if the application later changes from console output to a centralized logging destination.

Providers also differ in formatting, filtering capabilities, persistence, and support for structured data. Choose a destination based on operational requirements rather than assuming that every provider behaves identically.

### 9. Logging scopes

A logging scope attaches shared contextual information to a group of log events. This is useful when multiple events belong to the same operation.

For example:

```csharp
using (_logger.BeginScope(
    "SimulationId: {SimulationId}",
    simulationId))
{
    _logger.LogInformation("Beginning update.");
    _logger.LogInformation("Finished update.");
}
```

Both events occur within the same scope and can carry the simulation identifier when the configured provider supports scopes.

Scopes are particularly useful for request IDs, simulation IDs, job IDs, and other values that help connect related events.

A scope is disposed when execution leaves the `using` block, ending that contextual scope. Scopes can also be nested.

Whether scope information is displayed or retained depends on provider configuration. For example, the console provider's scope output may need to be enabled.

Do not use scopes as a substitute for clear message templates. Use them to attach context that genuinely applies to a related group of events.

### 10. Basic diagnostics and troubleshooting

Logging is most useful when it helps answer a specific question about an application's behavior.

Suppose a simulation fails to initialize. A useful investigation might ask:

1. Did the application start?
2. How many boids did it attempt to initialize?
3. Which configuration values were used?
4. Did initialization finish or fail?
5. Was an exception recorded?
6. Does the failure happen consistently or only under certain conditions?

Logging those meaningful milestones can narrow down the source of the failure.

For example:

```csharp
_logger.LogInformation("Initializing simulation.");

_logger.LogDebug(
    "Using {BoidCount} boids and speed {Speed}.",
    boidCount,
    speed);

try
{
    InitializeBoids(boidCount, speed);
    _logger.LogInformation("Simulation initialized.");
}
catch (Exception exception)
{
    _logger.LogError(
        exception,
        "Simulation initialization failed.");

    throw;
}
```

This example logs the start and successful completion of initialization, records useful parameters at `Debug` level, and logs the exception if initialization fails.

If the application appears to stop after the first message, the absence of the success message is a useful clue. It is not proof of the exact cause, but it helps narrow the investigation.

Logging should complement other diagnostic tools. A debugger can inspect values at a precise point in execution; profiling tools can help identify performance bottlenecks; and runtime diagnostic tools can expose process-level behavior that ordinary application logs may not capture.

### 11. Logging best practices

Good logging is selective, structured, and purposeful.

- **Choose meaningful levels.** Routine milestones belong at `Information`; detailed execution information usually belongs at `Debug` or `Trace`.
- **Use structured templates.** Prefer named placeholders over string interpolation for variable data in log messages.
- **Include exceptions when logging failures.** Pass the exception object so the provider can record its details.
- **Avoid duplicate logs.** Log a failure at the layer that can add useful context or make a meaningful handling decision.
- **Avoid excessive logging in hot paths.** Frequently executed loops can generate substantial output and affect performance.
- **Keep messages actionable.** Record what happened and the context needed to investigate it.
- **Protect sensitive data.** Do not record credentials, tokens, or sensitive user information.
- **Use filtering.** Configure levels by environment and category to control noise.
- **Do not use logging as program control flow.** Code should make decisions based on actual state and results, not on whether a message was written.
- **Distinguish expected conditions from failures.** Not every unsuccessful lookup or rejected input needs to be an error.

Logging is an operational tool, not a substitute for correct error handling. A well-designed application combines meaningful log events with clear failure behavior and appropriate recovery strategies.

## Challenge

Build a console program that demonstrates structured logging, log levels, exception logging, and configurable filtering using the .NET logging abstractions.

Your program must satisfy all of the following requirements:

1. **Set up logging:** Configure a console logger and create a logger for a dedicated simulation service.
2. **Use log levels:** Emit at least one message at `Debug`, `Information`, `Warning`, and `Error` level. Ensure the configuration allows you to observe the levels you intend to demonstrate.
3. **Use structured logging:** Include named properties such as `BoidCount` and `SimulationId` in message templates, and supply matching values.
4. **Log an exception:** Create a controlled failure, catch the exception, and log it with its exception object. Do not silently swallow the failure.
5. **Configure filtering:** Set the minimum log level through configuration rather than scattering level checks throughout the application.
6. **Use a logging scope:** Associate at least two related log events with a shared simulation identifier.
7. **Separate responsibilities:** Keep logging setup and simulation behavior in clearly identifiable methods or classes.
8. **Verify output:** Run the project and confirm which events are emitted at the configured minimum level.
9. **Keep the implementation focused:** Avoid adding a web server, external logging service, or unrelated application functionality.

Use the .NET logging abstractions rather than implementing a custom logger. Add the required NuGet package references and configuration setup for your console project as needed. The goal is to understand how application code produces structured events and how configuration and providers determine which events appear.
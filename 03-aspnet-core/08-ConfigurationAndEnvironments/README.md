# Configuration & Environments in Web Apps

## Micro Goal

Understand how ASP.NET Core web applications load configuration from multiple sources, use environment-specific settings, access configuration through dependency injection, and manage environment-dependent behavior without hardcoding deployment settings into application code.

## Concepts Covered

- ASP.NET Core configuration system
- `IConfiguration` and configuration providers
- `appsettings.json` and `appsettings.{Environment}.json`
- Configuration precedence and overrides
- Development, Staging, and Production environments
- `ASPNETCORE_ENVIRONMENT` and `DOTNET_ENVIRONMENT`
- Accessing configuration in `Program.cs` and endpoint handlers
- Strongly typed configuration with the Options pattern
- `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>`
- Configuration validation
- Environment variables and hierarchical configuration keys
- Separating application settings from source code
- Avoiding secrets in committed configuration files
- Testing configuration-dependent behavior

## Completion Criteria

- [ ] Explain the purpose of `IConfiguration` in an ASP.NET Core web app.
- [ ] Read settings from `appsettings.json`.
- [ ] Create and use environment-specific configuration files.
- [ ] Explain which configuration source wins when multiple sources define the same key.
- [ ] Determine the current hosting environment through `IWebHostEnvironment`.
- [ ] Override a setting with an environment variable.
- [ ] Bind a configuration section to a strongly typed options class.
- [ ] Validate required configuration values during application startup.
- [ ] Explain the differences between `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>`.
- [ ] Keep secrets and deployment-specific values out of source code.
- [ ] Build and run the application under different environments and verify the behavior.

## Lecture

### 1. Why web applications need configuration

An ASP.NET Core application often runs in multiple environments, such as development on a local machine, staging for pre-release verification, and production for real users.

The application may use different values in each environment. For example, it might display detailed diagnostic information during development but use a public-facing application name and more restrictive behavior in production.

Hardcoding these values directly into C# makes changes difficult and can lead to accidental exposure of environment-specific information. Configuration separates changeable settings from application logic.

Consider a web application with a name, a maximum number of boids, and a feature toggle:

```json
{
  "Simulation": {
    "Name": "Boids",
    "MaxBoids": 500,
    "EnableDiagnostics": true
  }
}
```

The JSON file stores values. The application reads those values at runtime rather than embedding them throughout the codebase.

Configuration is not a replacement for application logic. It supplies settings that influence that logic.

### 2. The ASP.NET Core configuration system

ASP.NET Core uses a configuration system that combines values from multiple providers. Common providers include:

- JSON files, such as `appsettings.json`.
- Environment-specific JSON files.
- Environment variables.
- Command-line arguments.
- Other providers registered by the application.

The `IConfiguration` interface provides access to the combined configuration. In a typical ASP.NET Core project created with `dotnet new web`, the default builder configures common providers for you.

A simplified example:

```csharp
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

string? applicationName =
    builder.Configuration["ApplicationName"];

app.MapGet("/", () => new
{
    Name = applicationName ?? "Unnamed application"
});

app.Run();
```

`builder.Configuration` is available before the application is built. Configuration can also be injected into services and endpoint handlers through dependency injection.

Configuration values are commonly represented as strings when accessed by key. The indexer returns `null` when a key is not present. For more complex values, use configuration binding or typed accessors rather than repeatedly parsing strings throughout the application.

### 3. `appsettings.json`

The file `appsettings.json` is the standard location for general application settings.

For example:

```json
{
  "Application": {
    "Name": "Boids API"
  },
  "Simulation": {
    "MaxBoids": 500,
    "DefaultSpeed": 2.0
  }
}
```

Nested JSON objects form hierarchical configuration keys. The `Application:Name` key refers to the `Name` property inside the `Application` section.

You can access values using the configuration indexer:

```csharp
string? name =
    builder.Configuration["Application:Name"];

string? maximumBoids =
    builder.Configuration["Simulation:MaxBoids"];
```

You can also retrieve a section:

```csharp
IConfigurationSection simulationSection =
    builder.Configuration.GetSection("Simulation");
```

A section represents a portion of the configuration hierarchy. It is useful when multiple related settings belong to the same feature.

Avoid hardcoding values in endpoint handlers when those values are intended to vary between environments. Centralizing settings makes the application's behavior easier to understand and change.

### 4. Environment-specific configuration files

ASP.NET Core supports configuration files whose names include the current environment:

- `appsettings.json`
- `appsettings.Development.json`
- `appsettings.Staging.json`
- `appsettings.Production.json`

The base file contains general settings. An environment-specific file overrides matching keys for that environment.

For example, `appsettings.json` might contain:

```json
{
  "Application": {
    "Name": "Boids API"
  },
  "Simulation": {
    "MaxBoids": 500
  }
}
```

The development file could override the limit:

```json
{
  "Simulation": {
    "MaxBoids": 100
  }
}
```

If the current environment is `Development`, the effective `Simulation:MaxBoids` value is `100`. Other values, such as `Application:Name`, remain available from the base file because the environment-specific file does not replace the entire configuration hierarchy.

This allows each environment to override only the settings that need to differ.

The default ASP.NET Core builder loads the base JSON file and then the environment-specific JSON file. You generally do not need to register these files manually in a standard web project.

### 5. Configuration precedence

Configuration providers have an order. When the same key is defined by multiple providers, the value from the provider with higher precedence wins.

With the default web application configuration, common sources are ordered so that later, higher-priority sources can override earlier ones. For the usual application settings, the important relationship is:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User secrets in Development, when configured
4. Environment variables
5. Command-line arguments

The exact provider list can be changed by the application, so this order should be understood as the default arrangement rather than an unchangeable rule.

Suppose the JSON file contains:

```json
{
  "Simulation": {
    "MaxBoids": 500
  }
}
```

An environment variable can override that setting without changing the file:

```bash
Simulation__MaxBoids=750 dotnet run
```

The double underscore (`__`) represents a configuration hierarchy separator in environment variables. It maps to the colon in `Simulation:MaxBoids`.

The effective value is `750` for that process, provided no higher-priority source overrides it.

This is useful in deployment environments where the same application artifact needs different settings without being rebuilt.

### 6. Hosting environments

ASP.NET Core uses a hosting environment to identify where an application is running. The standard environment names are:

- `Development`
- `Staging`
- `Production`

These names are conventions, not an exhaustive list. You can define other environment names, but your application must explicitly account for them where necessary.

The current environment can be accessed through `IWebHostEnvironment`:

```csharp
app.MapGet("/environment", (IWebHostEnvironment environment) =>
{
    return Results.Ok(new
    {
        Environment = environment.EnvironmentName,
        IsDevelopment = environment.IsDevelopment(),
        IsProduction = environment.IsProduction()
    });
});
```

The environment name can affect which configuration files load and which environment-specific behaviors are enabled.

For example, development may enable additional diagnostics, while production should avoid exposing internal exception details to clients.

Do not treat the environment name as a security boundary by itself. Production security still depends on authentication, authorization, safe configuration, and correct error handling.

### 7. Setting the environment locally

For local development, you can set the hosting environment before launching the application.

On Linux or macOS:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

On Bash, this syntax sets the environment variable for the command being executed.

For the generic .NET host, `DOTNET_ENVIRONMENT` is also relevant. With `WebApplication.CreateBuilder`, both variables can participate in environment selection, and their precedence can depend on the hosting API being used. For a typical ASP.NET Core web app using `WebApplication.CreateBuilder`, `DOTNET_ENVIRONMENT` takes precedence over `ASPNETCORE_ENVIRONMENT` when both are set.

To avoid ambiguity, set the intended environment variable explicitly and do not set conflicting values.

The environment name is read during application startup. Changing a shell variable after the process has started does not automatically change the running application's hosting environment.

### 8. Accessing configuration in endpoints and services

Minimal APIs support dependency injection directly in endpoint handlers. Configuration can be injected into a handler just like other registered services.

```csharp
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/settings", (IConfiguration configuration) =>
{
    int maxBoids = configuration.GetValue<int>(
        "Simulation:MaxBoids");

    return Results.Ok(new { MaxBoids = maxBoids });
});

app.Run();
```

`GetValue<int>` converts the configured value to an integer. If the setting is absent, the non-nullable value type's default value can be returned, which is `0` for `int`. That may not be an acceptable application default, so required settings should be validated rather than silently relying on type defaults.

Injecting `IConfiguration` is appropriate when reading a small number of settings. If a feature depends on several related settings, a strongly typed options class often produces clearer code.

### 9. Strongly typed configuration with the Options pattern

The Options pattern binds a configuration section to a C# class. This gives settings a defined structure and avoids scattering string keys throughout the application.

For example, define a class:

```csharp
public sealed class SimulationOptions
{
    public const string SectionName = "Simulation";

    public int MaxBoids { get; set; } = 500;

    public double DefaultSpeed { get; set; } = 2.0;
}
```

Register the options during startup:

```csharp
builder.Services
    .AddOptions<SimulationOptions>()
    .Bind(builder.Configuration.GetSection(
        SimulationOptions.SectionName));
```

The section in `appsettings.json` should use matching property names:

```json
{
  "Simulation": {
    "MaxBoids": 500,
    "DefaultSpeed": 2.0
  }
}
```

A handler can then receive `IOptions<SimulationOptions>`:

```csharp
app.MapGet("/simulation/settings",
    (IOptions<SimulationOptions> options) =>
    {
        SimulationOptions settings = options.Value;

        return Results.Ok(new
        {
            settings.MaxBoids,
            settings.DefaultSpeed
        });
    });
```

Remember to import `Microsoft.Extensions.Options` if the relevant namespace is not already available through global usings.

The Options pattern provides a typed contract for related settings. It is especially useful as configuration grows beyond a few individual values.

### 10. Validating configuration

A configuration value may exist and still be invalid. For example, a maximum boid count of `-10` is not useful for a simulation.

You can validate options during startup so the application fails early rather than operating with invalid settings.

For example:

```csharp
builder.Services
    .AddOptions<SimulationOptions>()
    .Bind(builder.Configuration.GetSection(
        SimulationOptions.SectionName))
    .Validate(
        options => options.MaxBoids > 0,
        "MaxBoids must be greater than zero.")
    .Validate(
        options => double.IsFinite(options.DefaultSpeed)
                   && options.DefaultSpeed >= 0,
        "DefaultSpeed must be finite and non-negative.")
    .ValidateOnStart();
```

`ValidateOnStart()` makes the options validation run during startup. Without startup validation, validation can otherwise occur when the options value is first accessed, depending on the options setup.

Validation should enforce actual requirements, not merely check that values have the expected data types. A speed can be a valid `double` and still violate the application's rules.

For more complex validation, options classes can implement `IValidatableObject`, or validation can be registered through other supported options validation mechanisms.

### 11. `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>`

ASP.NET Core provides several interfaces for consuming bound options.

- **`IOptions<T>`** provides a stable options value and is commonly used when configuration does not need to be refreshed during the process lifetime.
- **`IOptionsSnapshot<T>`** provides options values scoped to a dependency injection scope. It is commonly used in web requests, where each request gets its own snapshot of the options value.
- **`IOptionsMonitor<T>`** provides a current options value and supports change notifications when the underlying configuration source supports reloads.

`IOptionsSnapshot<T>` is scoped and should not be injected into a singleton service. A singleton that needs to observe supported configuration changes may use `IOptionsMonitor<T>` instead.

These interfaces are not interchangeable in every lifetime or reload scenario. Choose the simplest one that meets the feature's requirements. For most beginner applications with settings that do not need to change while running, `IOptions<T>` is a reasonable starting point.

### 12. Secrets and sensitive configuration

Configuration files are suitable for ordinary settings, but committed JSON files should not contain credentials, API keys, private connection strings, or other sensitive values.

For local development, ASP.NET Core user secrets can store secrets outside the project directory. User secrets are a development convenience, not an encrypted production secrets vault.

Production deployments commonly provide sensitive settings through environment variables, a managed secret store, or another secure configuration provider appropriate to the deployment environment.

Never expose secret values through a public endpoint or include them in logs. If a configuration section contains both public settings and secrets, do not return the entire section from an API response.

Environment variables are useful for deployment-specific configuration, but access to the machine or process environment still needs to be controlled. They should not be assumed to provide complete secret management on their own.

### 13. Configuration and application design

Configuration should describe how the application operates in a particular environment. It should not replace the application's core domain logic.

For example, the maximum permitted boid count might be a configuration setting, while the algorithm that calculates a boid's movement belongs in application logic.

Keep configuration access near application startup or within the services that need those settings. Strongly typed options help avoid passing `IConfiguration` throughout every part of the application.

A useful separation is:

- **Configuration:** Values supplied by files, environment variables, or other providers.
- **Options classes:** Typed representations of related settings.
- **Application services:** Logic that uses those settings.
- **Endpoints:** HTTP-facing handlers that invoke services and return responses.

This separation makes the application easier to test and reduces dependence on configuration keys throughout the codebase.

## Challenge

Build a small ASP.NET Core Minimal API that reports its active environment and exposes validated simulation settings.

Your application must satisfy all of the following requirements:

1. **Create a web app:** Use the ASP.NET Core Empty Web template or another Minimal API template. The application must run as a web server and expose HTTP endpoints.
2. **Create base configuration:** Add `appsettings.json` with an `Application` section containing a name and a `Simulation` section containing `MaxBoids` and `DefaultSpeed`.
3. **Create environment-specific configuration:** Add `appsettings.Development.json` that overrides at least one setting from the base file.
4. **Expose the environment:** Create a `GET /environment` endpoint that returns the active environment name and whether the application is running in Development or Production.
5. **Use strongly typed options:** Create a `SimulationOptions` class and bind it to the `Simulation` configuration section.
6. **Validate settings:** Require `MaxBoids` to be greater than zero and `DefaultSpeed` to be finite and non-negative. Validate the options at application startup.
7. **Expose simulation settings:** Create a `GET /simulation/settings` endpoint that returns the effective maximum boid count and default speed.
8. **Demonstrate configuration precedence:** Run the app with an environment variable that overrides `Simulation:MaxBoids`. Verify that the endpoint returns the overridden value without modifying the JSON files.
9. **Handle environments correctly:** Run the application in Development and Production and verify that the environment endpoint reflects the active environment.
10. **Keep secrets private:** Do not put credentials or secrets in the JSON files, and do not expose the entire configuration through an endpoint.
11. **Run and verify:** Use `dotnet run` and test the endpoints using a browser, `curl`, or another HTTP client. Confirm that the returned JSON values match the active configuration.
12. **Keep responsibilities clear:** Keep configuration definitions in JSON, typed settings in `SimulationOptions`, and HTTP handling in the endpoint definitions. Avoid unnecessary abstractions.

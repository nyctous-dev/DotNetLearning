# Configuration and Environments

## Micro Goal

Understand how .NET applications load configuration from multiple sources, access settings through the configuration system, and use environment-specific settings to change application behavior without modifying source code.

## Concepts Covered

- The .NET configuration system
- `IConfiguration` and configuration providers
- `appsettings.json`
- Environment-specific JSON files
- Configuration precedence and overrides
- Environment variables
- Command-line configuration
- Configuration keys and hierarchical values
- Binding configuration sections to strongly typed classes
- The Options pattern and `IOptions<T>`
- Development, staging, and production environments
- `IHostEnvironment` and `IWebHostEnvironment`
- User Secrets for local development
- Configuration validation and safe handling of sensitive values

## Completion Criteria

- [ ] Explain the purpose of the .NET configuration system.
- [ ] Create and read settings from `appsettings.json`.
- [ ] Load environment-specific settings from `appsettings.Development.json`.
- [ ] Explain how configuration providers override one another.
- [ ] Read hierarchical configuration values using `IConfiguration`.
- [ ] Bind a configuration section to a strongly typed class.
- [ ] Register and inject configuration using `IOptions<T>`.
- [ ] Identify the current application environment.
- [ ] Use environment variables to override configuration values.
- [ ] Explain why secrets should not be committed to source control.
- [ ] Complete the challenge with a working console application.

## Lecture

### 1. Why configuration exists

Applications frequently need values that can change without requiring code changes or a new build. Examples include:

- The name of the application or simulation.
- The maximum number of boids allowed.
- The initial movement speed.
- Logging verbosity.
- Database connection strings.
- API endpoints.
- Feature flags that enable or disable behavior.

Hardcoding these values directly into source code makes the application harder to maintain. A value that changes between development and production would require code edits if it were embedded directly in the program.

Configuration separates application settings from application logic.

For example, instead of writing:

```csharp
const int MaxBoids = 500;
```

you can store the setting in a configuration file and read it when the application starts.

Configuration is not the same as application state. Configuration describes how the application should operate; application state describes what is happening while it runs. A maximum boid count is configuration, while the current number of boids is runtime state.

### 2. The .NET configuration system

.NET provides a configuration abstraction through `IConfiguration`. It allows application code to read settings without needing to know exactly where those settings originated.

Configuration can come from multiple providers, including:

- JSON files.
- Environment variables.
- Command-line arguments.
- User Secrets during local development.
- External systems or custom providers.

A provider loads settings from a particular source. The configuration system combines the providers into a single hierarchy of keys and values.

This means application code can read a setting through `IConfiguration` regardless of whether the value came from a JSON file, an environment variable, or another configured source.

Modern .NET console projects can use the Generic Host to provide configuration, dependency injection, logging, and application lifetime management together. The Generic Host is particularly useful when an application grows beyond a small demonstration.

### 3. `appsettings.json`

A common configuration source is `appsettings.json`, a JSON file in the project directory.

For example, create an `appsettings.json` file with:

```json
{
  "Simulation": {
    "Name": "Boids",
    "MaxBoids": 500,
    "InitialSpeed": 2.5
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

The `Simulation` section contains settings related to the simulation. The `Logging` section configures logging behavior when the application uses a logging provider that reads these settings.

JSON uses objects to organize related values hierarchically. Here, `Simulation` contains `Name`, `MaxBoids`, and `InitialSpeed`.

The configuration system represents hierarchical paths using colon-separated keys. For example:

- `Simulation:Name`
- `Simulation:MaxBoids`
- `Simulation:InitialSpeed`
- `Logging:LogLevel:Default`

These keys allow code to access individual values without manually parsing JSON.

A JSON file does not automatically become part of every application's configuration. The project must load it through a configuration builder or host. A typical ASP.NET Core application sets this up automatically, while a plain console application may need explicit configuration setup.

### 4. Reading configuration with `IConfiguration`

A straightforward way to load JSON configuration in a console application is to use `ConfigurationBuilder`.

The relevant packages for a standalone console project can be added with:

```bash
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Microsoft.Extensions.Configuration.Binder
```

Then load the configuration file:

```csharp
using Microsoft.Extensions.Configuration;

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

string? simulationName = configuration["Simulation:Name"];

Console.WriteLine($"Simulation: {simulationName}");
```

`SetBasePath` determines where the configuration file is searched for. `AppContext.BaseDirectory` points to the application's base directory at runtime. In a console project, the JSON file must be copied to that location during build or publish.

If your JSON file is not copied to the output directory, the configuration builder will not find it there. You can configure the project to copy the file by adding the following to the `.csproj` file:

```xml
<ItemGroup>
  <None Update="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

`AddJsonFile` adds the JSON configuration provider. The `optional: false` argument means the application should fail if the file is missing. `reloadOnChange: true` requests reloading when the file changes, though an application must consider whether and how it responds to updated values.

The indexer `configuration["Simulation:Name"]` returns the value as a string, or `null` if the key is missing. For values that require a specific type, you can use typed accessors or bind a configuration section to a class.

### 5. Reading typed values and hierarchical settings

Configuration values are commonly represented as strings at the provider boundary. Use typed accessors when you need numeric or Boolean values.

```csharp
int maxBoids = configuration.GetValue<int>("Simulation:MaxBoids");
double initialSpeed = configuration.GetValue<double>("Simulation:InitialSpeed");

Console.WriteLine($"Maximum boids: {maxBoids}");
Console.WriteLine($"Initial speed: {initialSpeed}");
```

These extension methods are provided by the configuration binder package.

A missing or invalid required value can cause conversion or binding errors. Avoid silently substituting defaults for critical settings unless that behavior is intentional. For settings that are required, validate them at startup and report a useful error if they are absent or invalid.

You can also access a whole section:

```csharp
IConfigurationSection simulationSection =
    configuration.GetSection("Simulation");

string? name = simulationSection["Name"];
```

Sections provide a convenient way to group related configuration values and pass a smaller portion of the configuration tree to code that needs it.

### 6. Configuration precedence

.NET configuration can combine multiple providers. When providers define the same key, the value from the provider with higher precedence wins.

For example, a console application's configuration builder might load sources in this order:

```csharp
IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build();
```

In this setup:

1. `appsettings.json` supplies the baseline settings.
2. `appsettings.Development.json` overrides matching values for development.
3. Environment variables override matching values from the JSON files.

Later providers take precedence over earlier providers for the same key.

Suppose the base file contains:

```json
{
  "Simulation": {
    "MaxBoids": 500
  }
}
```

The development file contains:

```json
{
  "Simulation": {
    "MaxBoids": 100
  }
}
```

The effective value of `Simulation:MaxBoids` is `100` when both files are loaded and no later provider overrides it.

Configuration providers do not necessarily replace an entire JSON object as a unit. Values are combined by key, so settings that exist only in the base file remain available unless overridden.

The order of provider registration is therefore important. If you add environment variables before the JSON files, JSON may override the environment values instead.

### 7. Environment-specific configuration files

Applications often require different settings for development, staging, and production.

Common file names include:

- `appsettings.Development.json`
- `appsettings.Staging.json`
- `appsettings.Production.json`

Each file can override only the settings that differ from the base configuration.

For example, `appsettings.Development.json` could contain:

```json
{
  "Simulation": {
    "MaxBoids": 100,
    "InitialSpeed": 1.0
  }
}
```

The base `appsettings.json` can retain production-oriented or general defaults, while the development file adjusts values for local experimentation.

An environment-specific file is only loaded if the application explicitly adds it or uses a host that configures it automatically.

For a standalone console application, the environment can be read from the `DOTNET_ENVIRONMENT` variable and used to choose a file:

```csharp
string environment =
    Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? "Production";

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile(
        $"appsettings.{environment}.json",
        optional: true)
    .AddEnvironmentVariables()
    .Build();
```

This example defaults to `Production` when the environment variable is absent. The selected environment file is loaded after the base file, and environment variables are loaded last.

A Generic Host or ASP.NET Core application provides established environment-aware configuration conventions. When using those frameworks, prefer their built-in setup rather than duplicating it manually.

### 8. Environment variables

Environment variables allow deployment settings to be supplied without editing configuration files.

With the environment variable provider, hierarchical configuration keys use double underscores (`__`) instead of colons because colons are not portable across all environment-variable naming conventions.

For example, this environment variable maps to the configuration key `Simulation:MaxBoids`:

```bash
Simulation__MaxBoids=750
```

On Linux, you can supply it for a single command:

```bash
Simulation__MaxBoids=750 dotnet run
```

The application must have added the environment variable provider for the value to be read through `IConfiguration`. In the preceding configuration builder, `.AddEnvironmentVariables()` enables it.

Environment variables are useful for deployment-specific values such as endpoint addresses, feature flags, and secrets supplied by a deployment system. However, environment variables are not automatically secure: they may be exposed through process inspection, diagnostic tools, or operational logs. Use the secret-management facilities appropriate for your deployment environment.

Do not assume an environment variable has been applied merely because it exists in the shell. Confirm that the application loads the corresponding provider and that the key matches the configuration hierarchy.

### 9. Binding configuration to a class

Reading many individual keys can become repetitive. Configuration binding maps a section to a strongly typed object.

For example, define a class:

```csharp
public sealed class SimulationSettings
{
    public string Name { get; set; } = "Boids";

    public int MaxBoids { get; set; } = 500;

    public double InitialSpeed { get; set; } = 2.5;
}
```

Then bind the section:

```csharp
SimulationSettings settings =
    configuration.GetSection("Simulation")
        .Get<SimulationSettings>()
    ?? throw new InvalidOperationException(
        "Simulation configuration is missing.");

Console.WriteLine(settings.Name);
Console.WriteLine(settings.MaxBoids);
Console.WriteLine(settings.InitialSpeed);
```

The `Get<T>()` method is provided by the configuration binder package. It creates and populates an object using the matching configuration values.

The class gives the configuration a defined shape. IntelliSense and compile-time type checking can help when you use `settings.MaxBoids` instead of repeating a string key throughout the application.

Binding alone does not guarantee that the settings are valid. For example, a negative `MaxBoids` value may bind successfully even though it makes no sense for the simulation. Validate configuration values before using them.

### 10. The Options pattern

In applications that use dependency injection, the Options pattern is a common way to provide configuration to services.

Instead of passing `IConfiguration` everywhere, register a settings class and inject `IOptions<T>` into the service that needs it.

For example, with a Generic Host:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<SimulationSettings>()
    .Bind(builder.Configuration.GetSection("Simulation"))
    .Validate(
        settings => settings.MaxBoids > 0,
        "MaxBoids must be greater than zero.")
    .Validate(
        settings => settings.InitialSpeed >= 0,
        "InitialSpeed cannot be negative.")
    .ValidateOnStart();

builder.Services.AddTransient<SimulationReporter>();

using IHost host = builder.Build();

SimulationReporter reporter =
    host.Services.GetRequiredService<SimulationReporter>();

reporter.PrintSettings();

public sealed class SimulationReporter(
    IOptions<SimulationSettings> options)
{
    public void PrintSettings()
    {
        SimulationSettings settings = options.Value;

        Console.WriteLine($"Simulation: {settings.Name}");
        Console.WriteLine($"Maximum boids: {settings.MaxBoids}");
        Console.WriteLine($"Initial speed: {settings.InitialSpeed}");
    }
}
```

This example assumes the `SimulationSettings` class is defined in the same project and that the relevant hosting, options, and configuration packages are available. A modern .NET console application can use the Generic Host to combine configuration and dependency injection.

`IOptions<T>` provides a configured settings object. Its value is generally treated as a stable snapshot for the application's lifetime.

Other options interfaces serve different needs:

- `IOptions<T>` provides a standard settings instance.
- `IOptionsSnapshot<T>` provides scoped options, typically useful in web applications where configuration may change between scopes.
- `IOptionsMonitor<T>` supports observing updated options when the underlying configuration source supports reloads.

Use the simplest option interface that fits the application's requirements. For a small console application, directly reading `IConfiguration` can be enough; the Options pattern becomes more useful as the application grows and settings need to be injected into multiple services.

### 11. Development, staging, and production

The application environment describes the context in which the application is running. Typical environment names are `Development`, `Staging`, and `Production`.

The environment can influence:

- Which configuration files are loaded.
- Which diagnostic details are shown.
- Whether development-only tooling is enabled.
- Which external services are used.
- How errors are reported.

In a Generic Host application, `IHostEnvironment` provides the environment name and environment-related information. ASP.NET Core applications also expose `IWebHostEnvironment`, which extends host environment information with web-root information.

A simple environment check looks like this:

```csharp
if (host.Services
    .GetRequiredService<IHostEnvironment>()
    .IsDevelopment())
{
    Console.WriteLine("Development environment.");
}
else
{
    Console.WriteLine("Non-development environment.");
}
```

For a Generic Host, the environment is typically determined through `DOTNET_ENVIRONMENT`. ASP.NET Core also supports `ASPNETCORE_ENVIRONMENT`; when both variables are relevant, precedence depends on the hosting model and framework configuration. Avoid setting conflicting environment names.

Do not rely on a development environment for production behavior. Development tools and detailed error output can expose internal information. Production should use intentional configuration, appropriate logging, and secure error handling.

### 12. User Secrets and sensitive values

Configuration files are appropriate for ordinary settings, but secrets require special care. Examples include database passwords, API keys, and access tokens.

Do not commit real secrets to Git. A private repository is not a safe place for credentials, because repository access can change and committed secrets may remain in the Git history even after a file is edited.

For local development, .NET User Secrets provides a way to store development secrets outside the project directory. It is intended for development rather than as a production secret store.

For a supported project, initialize User Secrets with:

```bash
dotnet user-secrets init
```

Set a secret:

```bash
dotnet user-secrets set "ExternalService:ApiKey" "your-development-key"
```

The value is stored outside the source tree. In a Generic Host application, User Secrets are normally added automatically for development when the application has a configured user-secrets identifier. A plain `ConfigurationBuilder` console application must add the appropriate User Secrets provider explicitly if it needs to read them.

In production, use an appropriate secret-management mechanism, such as the platform's managed secret store or a deployment system that injects secrets securely.

User Secrets do not encrypt values as a production security solution. Their main purpose is to keep local development secrets out of source control.

### 13. Configuration validation

Configuration should be checked before the application depends on it. Invalid settings can cause confusing failures later, such as a simulation trying to allocate an invalid number of boids or using a negative speed.

A simple manual validation check is:

```csharp
if (settings.MaxBoids <= 0)
{
    throw new InvalidOperationException(
        "Simulation:MaxBoids must be greater than zero.");
}

if (settings.InitialSpeed < 0 ||
    !double.IsFinite(settings.InitialSpeed))
{
    throw new InvalidOperationException(
        "Simulation:InitialSpeed must be finite and non-negative.");
}
```

Validation should reflect the application's actual rules. For example, if zero boids is a valid configuration, `MaxBoids` should not be validated as strictly positive. If a speed must be finite and non-negative, checking only `speed < 0` is insufficient because floating-point values can also be `NaN` or infinity.

For applications using dependency injection, the Options pattern supports validation during startup. Startup validation is often preferable because the application fails early with a clear error instead of operating with invalid settings.

The goal is not to validate every possible configuration value indiscriminately. Validate values that are required or constrained by the application's behavior, and provide errors that identify the relevant setting.

## Challenge

Build a console application that loads and validates configuration for the Boids simulation. Use JSON configuration, environment-specific overrides, environment variables, and a strongly typed settings class.

Your program must satisfy all of the following requirements:

1. **Create base configuration:** Add an `appsettings.json` file containing a simulation name, maximum boid count, initial speed, and a Boolean indicating whether the simulation starts automatically.
2. **Load JSON configuration:** Use `IConfiguration` and `ConfigurationBuilder` to load the JSON file. Ensure the file is available in the application's output directory when running the project.
3. **Add environment-specific configuration:** Create `appsettings.Development.json` with different values for at least two settings. Load it after the base configuration file.
4. **Support environment variables:** Add the environment variable provider last and demonstrate overriding a setting such as `Simulation:MaxBoids` with `Simulation__MaxBoids`.
5. **Bind a settings class:** Create a `SimulationSettings` class and bind the `Simulation` section to it.
6. **Validate the settings:** Reject invalid maximum boid counts and negative or non-finite initial speeds. Display a clear error if configuration is invalid.
7. **Report the active environment:** Read `DOTNET_ENVIRONMENT` and use it to select the environment-specific file. Use a sensible default when the variable is absent.
8. **Print the effective configuration:** Display the final simulation name, maximum boid count, initial speed, auto-start setting, and active environment.
9. **Explain precedence:** Add a short code comment explaining why an environment variable overrides values from the JSON files.
10. **Handle missing configuration:** Ensure missing required configuration files or invalid required settings produce understandable errors rather than silently substituting arbitrary values.

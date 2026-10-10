
using Microsoft.Extensions.Configuration;

string environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") is { Length: > 0 } configuredEnvironment
    ? configuredEnvironment
    : "Production";

try
{
    IConfiguration configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile($"appsettings.{environment}.json", optional: true)
        // Later providers override earlier providers for matching keys.
        .AddEnvironmentVariables()
        .Build();

    SimulationSettings settings = configuration
        .GetSection("Simulation")
        .Get<SimulationSettings>()
        ?? throw new InvalidOperationException(
            "Required configuration section 'Simulation' is missing.");

    settings.Validate();

    Console.WriteLine($"Environment: {environment}");
    Console.WriteLine($"Simulation name: {settings.Name}");
    Console.WriteLine($"Maximum boids: {settings.MaxBoids.Value}");
    Console.WriteLine($"Initial speed: {settings.InitialSpeed.Value:F1}");
    Console.WriteLine($"Auto-start: {settings.AutoStart.Value}");
}
catch (FileNotFoundException exception)
{
    Console.Error.WriteLine($"Configuration file error: {exception.Message}");
    return;
}
catch (FormatException exception)
{
    Console.Error.WriteLine($"Configuration format error: {exception.Message}");
    return;
}
catch (InvalidOperationException exception)
{
    Console.Error.WriteLine($"Configuration error: {exception.Message}");
    return;
}

sealed class SimulationSettings
{
    public string? Name { get; set; }
    public int? MaxBoids { get; set; }
    public double? InitialSpeed { get; set; }
    public bool? AutoStart { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException(
                "Simulation:Name is required.");
        }

        if (MaxBoids is null or <= 0)
        {
            throw new InvalidOperationException(
                "Simulation:MaxBoids must be greater than zero.");
        }

        if (InitialSpeed is null ||
            !double.IsFinite(InitialSpeed.Value) ||
            InitialSpeed.Value < 0)
        {
            throw new InvalidOperationException(
                "Simulation:InitialSpeed must be finite and non-negative.");
        }

        if (AutoStart is null)
        {
            throw new InvalidOperationException(
                "Simulation:AutoStart must be specified.");
        }
    }
}



// dotnet add package Microsoft.Extensions.Configuration
// dotnet add package Microsoft.Extensions.Configuration.Json
// dotnet add package Microsoft.Extensions.Configuration.Binder
// dotnet add package Microsoft.Extensions.Configuration.EnvironmentVariables

// make appsettings.json
// make appsettings.Development.json

// <None Update="appsettings*.json">
//   <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
// </None>

// DOTNET_ENVIRONMENT=Development Simulation__MaxBoids=750 dotnet run
    // Environment: Development
    // Simulation name: Boids Simulation
    // Maximum boids: 750
    // Initial speed: 1.0
    // Auto-start: True

// dotnet run
    // Environment: Production
    // Simulation name: Boids Simulation
    // Maximum boids: 500
    // Initial speed: 2.5
    // Auto-start: False
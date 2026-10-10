
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(
    builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();

builder.Services.AddSingleton<SimulationService>();

using IHost host = builder.Build();

SimulationService simulation =
    host.Services.GetRequiredService<SimulationService>();

ILogger applicationLogger = host.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("Application");

try
{
    simulation.Run(simulationId: 42, boidCount: 12);
}
catch (InvalidOperationException)
{
    applicationLogger.LogInformation(
        "The application handled the expected simulation failure and will exit.");
}


// dotnet run
    // dbug: SimulationService[0]
    //     => SimulationId: 42
    //     Preparing simulation 42 with 12 boids.
    // info: SimulationService[0]
    //     => SimulationId: 42
    //     Simulation 42 started with 12 boids.
    // warn: SimulationService[0]
    //     => SimulationId: 42
    //     Simulation 42 has 12 boids, exceeding the recommended limit of 10.
    // fail: SimulationService[0]
    //     => SimulationId: 42
    //     Simulation 42 failed while updating 12 boids.
    //     System.InvalidOperationException: Controlled failure during simulation update.
    //         at SimulationService.UpdateSimulation() in /home/mahonri/Desktop/DotNetLearning/02-dotnet-fundamentals/07-LoggingAndDiagnostics/SimulationService.cs:line 63
    //         at SimulationService.Run(Int32 simulationId, Int32 boidCount) in /home/mahonri/Desktop/DotNetLearning/02-dotnet-fundamentals/07-LoggingAndDiagnostics/SimulationService.cs:line 47
    // info: Application[0]
    //     The application handled the expected simulation failure and will exit.
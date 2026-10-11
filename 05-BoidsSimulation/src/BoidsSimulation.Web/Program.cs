
using BoidsSimulation.Core.Models;
using BoidsSimulation.Core.Simulation;
using BoidsSimulation.Web.Components;
using BoidsSimulation.Web.Endpoints;
using Microsoft.Extensions.Configuration;
using BoidsSimulation.Application.Sessions;
using BoidsSimulation.Web.Services;
using BoidsSimulation.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Register Blazor services.
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// Load and validate simulation limits.
var simulationLimits = builder.Configuration
    .GetSection("SimulationLimits")
    .Get<SimulationLimits>()
    ?? throw new InvalidOperationException(
        "Missing SimulationLimits configuration.");

simulationLimits.Validate();

// Load and validate simulation settings.
var simulationSettings = builder.Configuration
    .GetSection("Simulation")
    .Get<SimulationSettings>()
    ?? throw new InvalidOperationException(
        "Missing Simulation configuration.");

simulationSettings.Validate(simulationLimits);

// Load initialization settings.
var initializationSettings = builder.Configuration
    .GetSection("SimulationInitialization")
    .Get<SimulationInitializationSettings>()
    ?? new SimulationInitializationSettings();

// Resolve the seed once for this application instance.
var seedProvider = new SimulationSeedProvider(
    initializationSettings.Seed);

// Create the initial population once.
var initializer = new BoidPopulationInitializer(
    simulationSettings,
    simulationLimits);

var initialPopulation = initializer.CreateInitialPopulation(
    seedProvider.Seed);

// Create the shared simulation state once.
var simulationState = new SimulationState(initialPopulation);

// Register configuration and the shared simulation objects.
builder.Services.AddSingleton(simulationLimits);
builder.Services.AddSingleton(simulationSettings);
builder.Services.AddSingleton(initializationSettings);
builder.Services.AddSingleton(seedProvider);
builder.Services.AddSingleton(simulationState);

// Register the simulation pipeline.
builder.Services.AddSingleton<SimulationStepper>();
builder.Services.AddSingleton<SimulationSession>();
builder.Services.AddSingleton<SimulationLoop>();

// Start the simulation with the ASP.NET Core host.
builder.Services.AddHostedService<SimulationBackgroundService>();
builder.Services.AddHostedService<SimulationSnapshotBroadcaster>();
builder.Services.AddSingleton<SimulationConnectionStatus>();

// Configure SignalR JSON serialization for System.Numerics.Vector2.
builder.Services
    .AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.IncludeFields = true;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

// Map API endpoints.
app.MapSimulationApi();

// Map the Blazor application.
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<SimulationHub>("/hubs/simulation");

app.Run();

// Makes the application entry point accessible to WebApplicationFactory tests.
public partial class Program { }


using BoidsSimulation.Core.Models;
using BoidsSimulation.Core.Simulation;
using BoidsSimulation.Web.Components;
using BoidsSimulation.Web.Endpoints;
using Microsoft.Extensions.Configuration;
using System.Numerics;
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
// If the section or Seed value is missing, the seed defaults to null.
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

// Create the shared simulation state.
var simulationState = new SimulationState(initialPopulation);

// Register the configuration and shared simulation objects.
builder.Services.AddSingleton(simulationLimits);
builder.Services.AddSingleton(simulationSettings);
builder.Services.AddSingleton(initializationSettings);
builder.Services.AddSingleton(seedProvider);
builder.Services.AddSingleton(simulationState);

// Create the initial population and authoritative simulation state.
builder.Services.AddSingleton<SimulationState>(_ =>
{
    var random = Random.Shared;
    var boids = new Boid[simulationSettings.InitialBoidCount];

    for (var i = 0; i < boids.Length; i++)
    {
        var position = new Vector2(
            random.NextSingle() * simulationSettings.WorldWidth,
            random.NextSingle() * simulationSettings.WorldHeight);

        var angle = random.NextSingle() * MathF.Tau;
        var velocity = new Vector2(
            MathF.Cos(angle),
            MathF.Sin(angle)) * simulationSettings.MaxSpeed;

        boids[i] = new Boid(i + 1, position, velocity);
    }

    return new SimulationState(boids);
});

// Register the simulation pipeline.
builder.Services.AddSingleton<SimulationStepper>();
builder.Services.AddSingleton<SimulationSession>();
builder.Services.AddSingleton<SimulationLoop>();

// Start the simulation with the ASP.NET Core host.
builder.Services.AddHostedService<SimulationBackgroundService>();

builder.Services.AddSignalR();

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


using BoidsSimulation.Core.Models;
using BoidsSimulation.Web.Components;

var builder = WebApplication.CreateBuilder(args);


var simulationLimits = builder.Configuration
    .GetSection("SimulationLimits")
    .Get<SimulationLimits>()
    ?? throw new InvalidOperationException(
        "Missing SimulationLimits configuration.");

simulationLimits.Validate();

var simulationSettings = builder.Configuration
    .GetSection("Simulation")
    .Get<SimulationSettings>()
    ?? throw new InvalidOperationException(
        "Missing Simulation configuration.");

simulationSettings.Validate(simulationLimits);

builder.Services.AddSingleton(simulationLimits);
builder.Services.AddSingleton(simulationSettings);


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

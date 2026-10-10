
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

app.MapGet("/", (IConfiguration configuration) =>
{
    return Results.Ok(new
    {
        Name = configuration["Application:Name"] ?? "Unnamed application"
    });
});

app.MapGet("/environment", (IWebHostEnvironment environment) =>
{
    return Results.Ok(new
    {
        Environment = environment.EnvironmentName,
        IsDevelopment = environment.IsDevelopment(),
        IsProduction = environment.IsProduction()
    });
});

app.MapGet(
    "/simulation/settings",
    (IOptions<SimulationOptions> options) =>
    {
        SimulationOptions settings = options.Value;

        return Results.Ok(new
        {
            settings.MaxBoids,
            settings.DefaultSpeed
        });
    });

app.Run();

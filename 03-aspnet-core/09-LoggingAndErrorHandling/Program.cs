using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddSingleton<BoidService>();

var app = builder.Build();

// Register early so downstream middleware and endpoints are covered.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        IExceptionHandlerFeature? feature =
            context.Features.Get<IExceptionHandlerFeature>();

        string traceId =
            Activity.Current?.Id ?? context.TraceIdentifier;

        ILogger<Program> logger =
            context.RequestServices.GetRequiredService<ILogger<Program>>();

        if (feature is not null)
        {
            logger.LogError(
                feature.Error,
                "Unhandled exception for trace {TraceId}",
                traceId);
        }

        context.Response.StatusCode =
            StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
        };

        problem.Extensions["traceId"] = traceId;

        await context.Response.WriteAsJsonAsync(problem);
    });
});

app.MapGet(
    "/boids/{id:int}",
    (int id, BoidService service, ILogger<Program> logger) =>
    {
        if (id <= 0)
        {
            logger.LogWarning(
                "Rejected invalid boid identifier {BoidId}",
                id);

            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid boid identifier.",
                detail: "The boid identifier must be a positive integer.");
        }

        Boid? boid = service.FindById(id);

        if (boid is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Boid not found.",
                detail: "No boid exists with the requested identifier.");
        }

        return Results.Ok(boid);
    });

// Deliberately failing endpoint for development and integration tests only.
if (app.Environment.IsDevelopment())
{
    app.MapGet("/test/failure", () =>
    {
        throw new InvalidOperationException(
            "Internal diagnostic details that must not reach the client.");
    });
}

app.Run();

public partial class Program { }
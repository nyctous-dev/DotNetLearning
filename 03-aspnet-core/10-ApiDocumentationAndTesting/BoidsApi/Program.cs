using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<BoidStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/boids", (BoidStore store) =>
    Results.Ok(store.GetAll()))
    .WithSummary("Get all boids")
    .WithDescription("Returns every boid currently held by the simulation API.")
    .Produces<Boid[]>(StatusCodes.Status200OK);

app.MapGet("/api/boids/{id:int}", (int id, BoidStore store) =>
    {
        Boid? boid = store.GetById(id);

        return boid is null
            ? Results.NotFound()
            : Results.Ok(boid);
    })
    .WithSummary("Get a boid by ID")
    .WithDescription("Returns a boid by its integer identifier, or 404 if it does not exist.")
    .Produces<Boid>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound);

app.MapPost("/api/boids", (CreateBoidRequest request, BoidStore store) =>
    {
        if (!double.IsFinite(request.X) ||
            !double.IsFinite(request.Y) ||
            !double.IsFinite(request.Speed) ||
            request.Speed < 0)
        {
            return Results.BadRequest(
                new ApiError(
                    "Coordinates must be finite, and speed must be finite and non-negative."));
        }

        Boid boid = store.Create(request);

        return Results.Created($"/api/boids/{boid.Id}", boid);
    })
    .WithSummary("Create a boid")
    .WithDescription("Creates a boid from finite coordinates and a finite, non-negative speed.")
    .Produces<Boid>(StatusCodes.Status201Created)
    .Produces<ApiError>(StatusCodes.Status400BadRequest);

app.Run();

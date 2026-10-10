using BoidsApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<BoidStore>();

var app = builder.Build();

app.MapGet("/api/boids", (BoidStore store) =>
    Results.Ok(store.GetAll()))
    .WithSummary("Get all boids")
    .WithDescription("Returns all boids currently held by the API.");

app.MapGet("/api/boids/{id:int}", (int id, BoidStore store) =>
    GetBoid(id, store))
    .WithSummary("Get a boid by ID")
    .WithDescription("Returns a boid by its identifier, or 404 if it does not exist.");

app.MapPost("/api/boids", (CreateBoidRequest request, BoidStore store) =>
    CreateBoid(request, store))
    .WithSummary("Create a boid")
    .WithDescription("Creates a boid with finite coordinates and non-negative finite speed.");

app.Run();

static IResult GetBoid(int id, BoidStore store)
{
    Boid? boid = store.GetById(id);

    return boid is null
        ? Results.NotFound()
        : Results.Ok(boid);
}

static IResult CreateBoid(CreateBoidRequest request, BoidStore store)
{
    if (!double.IsFinite(request.X) ||
        !double.IsFinite(request.Y) ||
        !double.IsFinite(request.Speed) ||
        request.Speed < 0)
    {
        return Results.BadRequest(
            new ApiError(
                "Coordinates must be finite and speed must be finite and non-negative."));
    }

    Boid boid = store.Create(request);

    return Results.Created($"/api/boids/{boid.Id}", boid);
}

public partial class Program { }
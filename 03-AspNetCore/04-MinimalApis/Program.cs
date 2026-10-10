
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<BoidStore>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

var boids = app.MapGroup("/boids");

boids.MapGet("", (BoidStore store) =>
    Results.Ok(store.GetAll()));

boids.MapGet("/{id:int}", (int id, BoidStore store) =>
{
    Boid? boid = store.GetById(id);

    return boid is null
        ? Results.NotFound(new { error = $"Boid {id} was not found." })
        : Results.Ok(boid);
});

boids.MapPost("", (CreateBoidRequest request, BoidStore store) =>
{
    string? error = BoidValidation.Validate(
        request.X,
        request.Y,
        request.Speed);

    if (error is not null)
    {
        return Results.BadRequest(new { error });
    }

    Boid boid = store.Create(request);

    return Results.Created($"/boids/{boid.Id}", boid);
});

boids.MapPut("/{id:int}", (
    int id,
    ReplaceBoidRequest request,
    BoidStore store) =>
{
    string? error = BoidValidation.Validate(
        request.X,
        request.Y,
        request.Speed);

    if (error is not null)
    {
        return Results.BadRequest(new { error });
    }

    if (!store.Replace(id, request))
    {
        return Results.NotFound(new { error = $"Boid {id} was not found." });
    }

    return Results.NoContent();
});

boids.MapPatch("/{id:int}", (
    int id,
    PatchBoidRequest request,
    BoidStore store) =>
{
    string? error = BoidValidation.Validate(request);

    if (error is not null)
    {
        return Results.BadRequest(new { error });
    }

    if (!store.Patch(id, request))
    {
        return Results.NotFound(new { error = $"Boid {id} was not found." });
    }

    return Results.Ok(store.GetById(id));
});

boids.MapDelete("/{id:int}", (int id, BoidStore store) =>
    store.Delete(id)
        ? Results.NoContent()
        : Results.NotFound(new { error = $"Boid {id} was not found." }));

app.Run();
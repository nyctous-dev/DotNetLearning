var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

List<Boid> boids =
[
    new(1, 10.0, 20.0, 2.5, true),
    new(2, 15.0, 25.0, 1.8, false),
    new(3, 30.0, 12.0, 3.0, true)
];

int nextId = boids.Max(boid => boid.Id) + 1;

var group = app.MapGroup("/api/boids");

group.MapGet("/", (bool? active) =>
{
    IEnumerable<Boid> result = boids;

    if (active.HasValue)
    {
        result = result.Where(boid => boid.Active == active.Value);
    }

    return Results.Ok(result);
});

group.MapGet("/{id:int}", (int id) =>
{
    if (id <= 0)
    {
        return (IResult)Results.BadRequest(
            new { error = "The boid ID must be positive." });
    }

    Boid? boid = boids.FirstOrDefault(boid => boid.Id == id);

    return boid is null
        ? Results.NotFound()
        : Results.Ok(boid);
});

group.MapPost("/", (CreateBoidRequest request) =>
{
    if (!double.IsFinite(request.X) ||
        !double.IsFinite(request.Y) ||
        !double.IsFinite(request.Speed) ||
        request.Speed < 0)
    {
        return Results.BadRequest(new
        {
            error = "Coordinates must be finite and speed must be finite and non-negative."
        });
    }

    Boid boid = new(
        nextId++,
        request.X,
        request.Y,
        request.Speed,
        request.Active);

    boids.Add(boid);

    return Results.Created($"/api/boids/{boid.Id}", boid);
});

group.MapDelete("/{id:int}", (int id) =>
{
    if (id <= 0)
    {
        return Results.BadRequest(
            new { error = "The boid ID must be positive." });
    }

    Boid? boid = boids.FirstOrDefault(boid => boid.Id == id);

    if (boid is null)
    {
        return Results.NotFound();
    }

    boids.Remove(boid);
    return Results.NoContent();
});

app.Run();
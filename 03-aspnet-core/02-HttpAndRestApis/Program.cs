using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var boids = new Dictionary<int, Boid>();
int nextId = 1;

app.MapGet("/api/boids", GetBoids);
app.MapGet("/api/boids/{id:int}", GetBoid);
app.MapPost("/api/boids", CreateBoid);
app.MapPut("/api/boids/{id:int}", ReplaceBoid);
app.MapDelete("/api/boids/{id:int}", DeleteBoid);

app.Run();

IResult GetBoids(int? limit)
{
    if (limit is <= 0)
    {
        return Results.BadRequest(new
        {
            error = "The limit must be greater than zero."
        });
    }

    IEnumerable<Boid> result = boids.Values;

    if (limit is int count)
    {
        result = result.Take(count);
    }

    return Results.Ok(result);
}

IResult GetBoid(int id)
{
    return boids.TryGetValue(id, out Boid? boid)
        ? Results.Ok(boid)
        : Results.NotFound(new { error = $"Boid {id} was not found." });
}

IResult CreateBoid(CreateBoidRequest request)
{
    if (!IsValidSpeed(request.Speed))
    {
        return Results.BadRequest(new
        {
            error = "Speed must be finite and non-negative."
        });
    }

    var boid = new Boid(
        nextId++,
        request.X,
        request.Y,
        request.Speed);

    boids.Add(boid.Id, boid);

    return Results.Created($"/api/boids/{boid.Id}", boid);
}

IResult ReplaceBoid(int id, CreateBoidRequest request)
{
    if (!boids.ContainsKey(id))
    {
        return Results.NotFound(new { error = $"Boid {id} was not found." });
    }

    if (!IsValidSpeed(request.Speed))
    {
        return Results.BadRequest(new
        {
            error = "Speed must be finite and non-negative."
        });
    }

    var boid = new Boid(id, request.X, request.Y, request.Speed);
    boids[id] = boid;

    return Results.Ok(boid);
}

IResult DeleteBoid(int id)
{
    return boids.Remove(id)
        ? Results.NoContent()
        : Results.NotFound(new { error = $"Boid {id} was not found." });
}

static bool IsValidSpeed(double speed) =>
    double.IsFinite(speed) && speed >= 0;
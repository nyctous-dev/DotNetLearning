var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MovementService>();
builder.Services.AddSingleton<SimulationStatusService>();
builder.Services.AddScoped<RequestMarker>();

var app = builder.Build();

app.MapGet(
    "/movement-speed",
    (double baseSpeed, double multiplier, MovementService movement) =>
    {
        try
        {
            double speed = movement.CalculateMovementSpeed(
                baseSpeed,
                multiplier);

            return Results.Ok(new { baseSpeed, multiplier, speed });
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return Results.BadRequest(new { error = exception.Message });
        }
        catch (OverflowException exception)
        {
            return Results.BadRequest(new { error = exception.Message });
        }
    });

app.MapGet(
    "/status",
    (SimulationStatusService status) =>
        Results.Ok(new { status = status.GetStatus() }));

app.MapGet(
    "/marker/compare",
    (RequestMarker first, RequestMarker second) =>
        Results.Ok(new
        {
            firstId = first.Id,
            secondId = second.Id,
            sameInstance = ReferenceEquals(first, second)
        }));

app.Run();
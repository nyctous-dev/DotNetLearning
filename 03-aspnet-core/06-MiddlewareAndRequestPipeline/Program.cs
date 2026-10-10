var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// 1. Log request details before downstream processing.
app.Use(async (context, next) =>
{
    app.Logger.LogInformation(
        "Request started: {Method} {Path}",
        context.Request.Method,
        context.Request.Path);

    await next(context);
});

// 2. Measure downstream processing time.
app.Use(async (context, next) =>
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    await next(context);

    stopwatch.Stop();

    app.Logger.LogInformation(
        "Request completed: {Method} {Path}, Status {StatusCode}, Elapsed {ElapsedMilliseconds} ms",
        context.Request.Method,
        context.Request.Path,
        context.Response.StatusCode,
        stopwatch.ElapsedMilliseconds);
});

// 3. Add a response header after downstream processing.
app.Use(async (context, next) =>
{
    await next(context);

    if (!context.Response.HasStarted)
    {
        context.Response.Headers["X-Application"] = "Boids";
    }
});

// 4. Short-circuit maintenance requests.
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/maintenance")
    {
        context.Response.StatusCode =
            StatusCodes.Status503ServiceUnavailable;

        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync(
            "Service temporarily unavailable for maintenance.");

        return;
    }

    await next(context);
});

// 5. Endpoints.
app.MapGet("/", () => "Hello from the Boids application!");
app.MapGet("/status", () => Results.Ok(new { status = "healthy" }));

app.Run();

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Middleware 1: log request details and the final response status.
app.Use(async (context, next) =>
{
    Console.WriteLine(
        $"Request: {context.Request.Method} {context.Request.Path}");

    await next(context);

    Console.WriteLine(
        $"Response status: {context.Response.StatusCode}");
});

// Middleware 2: demonstrate the order of execution.
app.Use(async (context, next) =>
{
    Console.WriteLine("Second middleware: before next");

    await next(context);

    Console.WriteLine("Second middleware: after next");
});

// Middleware 3: short-circuit requests to /blocked.
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/blocked")
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsync("Request blocked.");
        return;
    }

    await next(context);
});

// Endpoints.
app.MapGet("/", () => "Hello from ASP.NET Core!");

app.MapGet("/status", () =>
    Results.Ok(new { status = "running" }));

app.Run();

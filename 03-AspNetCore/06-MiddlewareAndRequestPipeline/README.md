# Middleware & Request Pipeline

## Micro Goal

Understand how ASP.NET Core processes HTTP requests through an ordered middleware pipeline, how middleware can inspect or modify requests and responses, and how to implement custom middleware that performs a focused task.

## Concepts Covered

- The ASP.NET Core request-processing pipeline
- Middleware and the `RequestDelegate` model
- Registering middleware with `Use`, `Run`, and `Map`
- Middleware execution order
- Request delegation and short-circuiting
- Request and response processing
- Custom middleware using `app.Use`
- Custom middleware classes
- The `HttpContext` object
- Request paths, methods, headers, and response status codes
- `await next(context)` and response timing
- Exception-handling middleware
- Middleware branching with `Map`
- Built-in middleware and endpoint execution
- Middleware placement relative to routing and endpoints
- Testing middleware behavior

## Completion Criteria

- [ ] Explain how an HTTP request moves through the ASP.NET Core middleware pipeline.
- [ ] Distinguish `Use`, `Run`, and `Map`.
- [ ] Demonstrate how middleware order affects request and response behavior.
- [ ] Implement middleware that executes code before and after the next component.
- [ ] Implement middleware that short-circuits a request.
- [ ] Read request information from `HttpContext`.
- [ ] Modify response headers and status codes through middleware.
- [ ] Create a reusable custom middleware class.
- [ ] Explain why exception-handling middleware must be placed early in the pipeline.
- [ ] Place middleware correctly relative to routing and endpoint execution.
- [ ] Verify middleware behavior by sending HTTP requests and inspecting responses.

## Lecture

### 1. What middleware is

ASP.NET Core processes HTTP requests through a sequence of components called the **middleware pipeline**. Each middleware component can inspect a request, perform work, pass the request to the next component, and inspect or modify the response as it travels back.

Middleware is the mechanism ASP.NET Core uses for cross-cutting HTTP concerns such as exception handling, request logging, authentication, authorization, and response compression.

A simplified pipeline looks like this:

```text
HTTP Request
     |
     v
Middleware A
     |
     v
Middleware B
     |
     v
Endpoint
     |
     v
Middleware B resumes
     |
     v
Middleware A resumes
     |
     v
HTTP Response
```

The request travels forward through the pipeline. If a middleware component calls the next component, execution continues. After the next component finishes, the earlier middleware can resume and perform additional work.

Not every middleware component calls the next one. Some deliberately finish the response themselves, preventing later components from executing. This behavior is called **short-circuiting**.

### 2. The `Program.cs` hosting model

In a modern ASP.NET Core application, `Program.cs` commonly creates a `WebApplicationBuilder`, builds a `WebApplication`, registers middleware, maps endpoints, and starts the server.

A minimal application looks like this:

```csharp
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "Hello from ASP.NET Core!");

app.Run();
```

Here:

- `WebApplication.CreateBuilder(args)` creates the application builder and loads the default configuration and hosting setup.
- `builder.Build()` creates the application.
- `app.MapGet(...)` registers an HTTP GET endpoint.
- `app.Run()` starts the web application and blocks while the host is running.

The final `app.Run()` is the hosting operation. It is different from the middleware extension `app.Run(...)`, which registers terminal middleware. That distinction matters because the two calls have different purposes.

### 3. Registering middleware with `Use`

The `Use` method registers middleware that can perform work before and after the next component.

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.Use(async (context, next) =>
{
    Console.WriteLine("Before next middleware");

    await next(context);

    Console.WriteLine("After next middleware");
});

app.MapGet("/", () => "Hello from ASP.NET Core!");

app.Run();
```

The `context` parameter is an `HttpContext`, which represents the current HTTP request and response. The `next` parameter represents the next component in the pipeline.

The call `await next(context)` passes control to the next component and asynchronously waits for it to finish. Once it completes, execution resumes after that statement.

For a request that reaches the endpoint successfully, the console output is:

```text
Before next middleware
After next middleware
```

The endpoint produces the HTTP response between those two log messages.

Use `await` so asynchronous work downstream can complete without blocking a thread unnecessarily. Middleware should generally return control only after the work it is responsible for has completed.

### 4. The difference between `Use`, `Run`, and `Map`

These three methods serve different purposes.

**`Use`** registers middleware that can call the next component:

```csharp
app.Use(async (context, next) =>
{
    Console.WriteLine("Request received");

    await next(context);
});
```

This middleware continues the pipeline by invoking `next`.

**`Run`** registers terminal middleware. It does not accept a `next` delegate:

```csharp
app.Run(async context =>
{
    context.Response.ContentType = "text/plain";
    await context.Response.WriteAsync("Request handled here.");
});
```

This component handles the request and does not delegate to later middleware.

**`Map`** creates a branch based on a request path prefix:

```csharp
app.Map("/diagnostics", diagnostics =>
{
    diagnostics.Run(async context =>
    {
        await context.Response.WriteAsync("Diagnostics branch");
    });
});
```

A request to `/diagnostics` enters this branch. A request to `/` does not enter it.

These methods are not interchangeable. Use `Use` for processing that should continue through the pipeline, terminal `Run` when a component should finish the request, and `Map` when a path prefix should lead to a separate branch.

### 5. Middleware order

Middleware is executed in the order in which it is registered. This order determines which components see a request and which components can observe or modify the response.

Consider this example:

```csharp
app.Use(async (context, next) =>
{
    Console.WriteLine("A: before");
    await next(context);
    Console.WriteLine("A: after");
});

app.Use(async (context, next) =>
{
    Console.WriteLine("B: before");
    await next(context);
    Console.WriteLine("B: after");
});

app.Run(async context =>
{
    Console.WriteLine("Terminal component");
    await context.Response.WriteAsync("Done");
});
```

The output is:

```text
A: before
B: before
Terminal component
B: after
A: after
```

The first middleware enters first and resumes last. This nested execution model is sometimes called the *onion model* of middleware.

Middleware order also affects behavior when a component short-circuits. If an earlier component completes the response without calling the next component, every component registered after it is skipped for that request.

For this reason, middleware registration order is a functional part of application design, not merely an organizational preference.

### 6. The `HttpContext` object

Each HTTP request is represented by an `HttpContext`. Middleware uses it to access request information, produce a response, and interact with request-scoped features.

Common members include:

- `context.Request.Method` — the HTTP method, such as `GET` or `POST`.
- `context.Request.Path` — the request path.
- `context.Request.Headers` — request headers.
- `context.Response.StatusCode` — the response status code.
- `context.Response.Headers` — response headers.
- `context.Response.ContentType` — the response content type.
- `context.Response.WriteAsync(...)` — writes text to the response body.

For example:

```csharp
app.Use(async (context, next) =>
{
    Console.WriteLine(
        $"{context.Request.Method} {context.Request.Path}");

    await next(context);
});
```

This middleware writes the HTTP method and path to the console before passing the request along.

`HttpContext` represents one request. Do not store it in a static field or retain it for use after the request has completed. If data needs to survive beyond the request, copy the required values into an appropriate object with a suitable lifetime.

### 7. Modifying responses

Middleware can modify response headers and status codes. When middleware awaits the next component, it can add or adjust headers afterward as long as the response has not started.

```csharp
app.Use(async (context, next) =>
{
    await next(context);

    if (!context.Response.HasStarted)
    {
        context.Response.Headers["X-Application"] = "Boids";
    }
});
```

This middleware adds a custom response header after downstream processing. The `HasStarted` check matters because headers cannot generally be changed after the response has begun to be sent to the client.

Middleware can also inspect the response status code after downstream processing:

```csharp
app.Use(async (context, next) =>
{
    await next(context);

    Console.WriteLine(
        $"Response status: {context.Response.StatusCode}");
});
```

The status code will reflect the response state after the downstream component completes. Middleware should avoid overwriting status codes or headers without a clear reason, since doing so can interfere with endpoint behavior.

### 8. Short-circuiting the pipeline

Sometimes middleware should reject or handle a request without executing later components.

For example, a simple maintenance gate could return a response immediately:

```csharp
app.Use(async (context, next) =>
{
    bool maintenanceMode = false;

    if (maintenanceMode)
    {
        context.Response.StatusCode =
            StatusCodes.Status503ServiceUnavailable;

        await context.Response.WriteAsync(
            "Service temporarily unavailable.");

        return;
    }

    await next(context);
});
```

When maintenance mode is enabled, the middleware writes a `503 Service Unavailable` response and returns without invoking `next`. The remaining pipeline is skipped.

This illustrates a general principle: middleware controls whether the request proceeds. Short-circuiting is appropriate when a component can fully handle the request or deliberately reject it.

A production maintenance mode would normally be driven by configuration rather than a hard-coded Boolean.

### 9. Custom middleware classes

Inline middleware is useful for small examples. When logic grows or needs to be reused, place it in a dedicated class.

A conventional custom middleware class receives a `RequestDelegate` in its constructor and implements an `InvokeAsync` method:

```csharp
using Microsoft.AspNetCore.Http;

public sealed class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(
        RequestDelegate next,
        ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();

        _logger.LogInformation(
            "{Method} {Path} completed with status {StatusCode} in {ElapsedMilliseconds} ms",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }
}
```

Register it with the application:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseMiddleware<RequestTimingMiddleware>();

app.MapGet("/", () => "Hello");

app.Run();
```

The `RequestDelegate` represents the next component in the pipeline. ASP.NET Core supplies it when constructing the middleware.

The `ILogger<T>` dependency is provided through ASP.NET Core's dependency injection system. The middleware records the method, path, status code, and elapsed time after the downstream pipeline completes.

This example demonstrates a timing measurement for the downstream processing. It does not include the entire network round trip, such as the client's network latency.

Conventional middleware instances are generally constructed when the pipeline is built and reused across requests. Therefore, do not store request-specific mutable state in instance fields. Keep per-request values in local variables or in the `HttpContext`.

### 10. Middleware and dependency injection

ASP.NET Core middleware commonly uses dependency injection to obtain services such as loggers. Conventional middleware is instantiated as part of pipeline setup, so constructor-injected services generally need to be suitable for that lifetime.

For services that are scoped to a request, inject them into `InvokeAsync` parameters instead of storing them in the middleware constructor:

```csharp
public sealed class ExampleMiddleware
{
    private readonly RequestDelegate _next;

    public ExampleMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IExampleRequestService requestService)
    {
        requestService.ProcessRequest(context.Request.Path);

        await _next(context);
    }
}
```

In this example, `IExampleRequestService` is illustrative; the application must define and register a concrete implementation. ASP.NET Core resolves the invocation parameter for the request.

The principle is to align service usage with its intended lifetime. Do not capture a scoped service in a long-lived middleware instance.

### 11. Exception-handling middleware

Exceptions can occur while middleware or endpoints process a request. Exception-handling middleware can catch unhandled exceptions from downstream components and translate them into an appropriate HTTP response.

ASP.NET Core provides built-in exception handling support. In production, configure it early in the pipeline so it can observe exceptions thrown by later middleware and endpoints.

A simplified setup is:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}

app.MapGet("/", () => "Hello");

app.Map("/error", error =>
{
    error.Run(async context =>
    {
        context.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsync(
            "An unexpected error occurred.");
    });
});

app.Run();
```

When an unhandled exception occurs downstream in a non-development environment, `UseExceptionHandler` redirects processing to the configured error path using ASP.NET Core's exception-handling mechanism.

The example keeps the response generic rather than exposing exception details to the client. In production, detailed exception messages and stack traces should be recorded securely in logs, not returned to the public response.

Exception handling should generally be registered before middleware that might throw, because it can only handle exceptions that propagate back through it. An exception thrown by middleware registered before the exception handler cannot be caught by that later handler.

### 12. Middleware, routing, and endpoints

Middleware interacts with routing and endpoint execution. In applications that use explicit routing, `UseRouting` selects the endpoint and `UseEndpoints` executes it. In the modern minimal hosting model, endpoint mapping methods such as `MapGet` integrate endpoint registration into the application pipeline.

The following example uses the explicit routing model to make the ordering clear:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseRouting();

app.Use(async (context, next) =>
{
    Console.WriteLine(
        $"Selected endpoint: {context.GetEndpoint()?.DisplayName}");

    await next(context);
});

app.UseEndpoints(endpoints =>
{
    endpoints.MapGet("/", async context =>
    {
        await context.Response.WriteAsync("Home");
    });
});

app.Run();
```

`UseRouting` selects an endpoint based on the request. The middleware after routing can inspect the selected endpoint through `context.GetEndpoint()`. `UseEndpoints` executes the selected endpoint.

In modern minimal hosting, the same application can commonly be written with `app.MapGet("/")`. Explicit `UseRouting` and `UseEndpoints` are not required for every minimal API application, but understanding their roles helps explain middleware ordering.

Some middleware needs to run before routing; other middleware needs the selected endpoint metadata. Middleware that depends on endpoint selection should be placed after routing. Authorization, for example, must be configured in the appropriate position relative to routing and endpoint execution so it can enforce the intended policies.

### 13. Middleware branching with `Map`

`Map` branches the pipeline based on a path prefix. This can be useful for isolated diagnostic routes or separate request-processing paths.

```csharp
app.Map("/health-check", branch =>
{
    branch.Run(async context =>
    {
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync("Healthy");
    });
});
```

A request to `/health-check` enters this branch and receives the response. Requests that do not match the branch continue through the main pipeline.

`Map` is different from endpoint routing. It branches middleware based on a path prefix, while `MapGet`, `MapPost`, and related endpoint mapping methods register endpoints for particular HTTP methods and routes.

Choose endpoint routing for ordinary HTTP API endpoints. Use middleware branching when a distinct pipeline branch is genuinely useful.

### 14. Testing middleware behavior

Middleware should be tested by verifying its observable behavior, not only by reading its implementation.

For example, if middleware adds a response header, send a request and check that the header is present. If middleware short-circuits a request, verify that it returns the expected status code and body, and that the downstream endpoint was not executed.

A lightweight manual check can use `curl` while the application is running:

```bash
curl -i http://localhost:5000/
```

The actual port depends on the application's launch settings and environment. Inspect the console output when starting the application to find the listening URL.

The `-i` option includes response headers, allowing you to inspect status codes and custom headers.

For automated tests, ASP.NET Core supports integration testing with `Microsoft.AspNetCore.Mvc.Testing` and `WebApplicationFactory<TEntryPoint>`. Those tools can host the application in a test environment and send requests without requiring a separate manually started server. They are useful for testing middleware order, headers, status codes, exception handling, and endpoint integration.

## Challenge

Build a small ASP.NET Core application that demonstrates the middleware request pipeline. Use a minimal API and implement custom middleware that logs request details, measures downstream processing time, adds a response header, and short-circuits a specific request.

Your application must satisfy all of the following requirements:

1. **Create a web application:** Use the ASP.NET Core minimal hosting model in `Program.cs`.
2. **Request logging middleware:** Record the HTTP method and path before passing the request to the next component.
3. **Response timing:** Measure how long downstream processing takes and log the elapsed time and final response status code after the next component completes.
4. **Response header middleware:** Add a custom header named `X-Application` with the value `Boids` when the response has not started.
5. **Short-circuiting:** When a request is made to `/maintenance`, return status code `503 Service Unavailable` with a clear response body without executing the downstream endpoint.
6. **Endpoints:** Define a `GET /` endpoint that returns a short message and a `GET /status` endpoint that returns a simple status response.
7. **Middleware order:** Ensure the request logging, timing, and response header middleware are registered in an order that produces the intended behavior.
8. **Asynchronous execution:** Await downstream middleware and asynchronous response writes correctly.
9. **Verify behavior:** Run the application and use `curl -i` or an equivalent HTTP client to verify the normal endpoint responses, the custom header, and the maintenance response.
10. **Keep responsibilities clear:** Use inline middleware for small pieces of pipeline logic or create a custom middleware class if that makes the implementation clearer. Avoid unrelated abstractions.

Be able to explain the order in which middleware executes for a normal request and why the maintenance request does not reach the endpoints.
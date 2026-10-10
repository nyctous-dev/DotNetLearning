# ASP.NET Core Fundamentals

## Micro Goal

Understand how an ASP.NET Core web application starts, configures its host, receives HTTP requests, processes them through the request pipeline, and returns HTTP responses.

## Concepts Covered

- ASP.NET Core and its role in web applications
- Web hosting and the application host
- The `WebApplication` builder
- `Program.cs` and application startup
- Registering application services
- Building and running the web application
- HTTP requests and responses
- The request-processing pipeline
- Middleware and request delegates
- Endpoint registration and execution
- Middleware ordering and short-circuiting
- Development and production behavior
- Running and accessing a local web server

## Completion Criteria

- [ ] Explain the role of ASP.NET Core in a web application.
- [ ] Describe what happens when an ASP.NET Core application starts.
- [ ] Explain the purpose of `WebApplication.CreateBuilder(args)`.
- [ ] Distinguish service registration from building and running the application.
- [ ] Explain how an HTTP request enters and moves through the request pipeline.
- [ ] Distinguish middleware from an endpoint.
- [ ] Demonstrate how middleware can process a request and response.
- [ ] Explain why middleware ordering matters.
- [ ] Create and run a minimal ASP.NET Core web application.
- [ ] Send an HTTP request to the application and inspect its response.
- [ ] Explain how development and production environments can affect application behavior.

## Lecture

### 1. What ASP.NET Core is

ASP.NET Core is a framework for building web applications and HTTP services with .NET. It provides the infrastructure needed to listen for HTTP requests, process those requests, and send responses back to clients.

A client might be a browser, a mobile application, another server, or a command-line HTTP client. The client sends a request to a server address, and the server processes it and returns a response.

For example, a browser might request:

```text
GET http://localhost:5000/
```

The server could respond with:

```text
HTTP/1.1 200 OK
Content-Type: text/plain

Hello from ASP.NET Core!
```

The request contains an HTTP method and a target URL. The response contains a status code, headers, and potentially a body.

ASP.NET Core provides the web server integration, request pipeline, routing infrastructure, dependency injection integration, configuration, and other facilities that allow the application to handle requests. You define the application-specific behavior, such as which URLs it supports and what information it returns.

ASP.NET Core can host server-rendered web applications, APIs, and other HTTP-based applications. Later, the Blazor learning area will build on these web fundamentals.

### 2. The web host and application startup

An ASP.NET Core application needs a host to manage its lifetime and coordinate the infrastructure required to serve requests.

The host brings together application configuration, registered services, logging, and server integration. In the common modern hosting model, ASP.NET Core uses `WebApplication` and `WebApplicationBuilder` to configure and run the application.

The `Program.cs` file is the application's startup entry point. A minimal web application can begin with:

```csharp
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "Hello from ASP.NET Core!");

app.Run();
```

This is a complete, minimal ASP.NET Core application.

The statements perform different jobs:

- `WebApplication.CreateBuilder(args)` creates a builder and loads the default hosting configuration, logging, and service-registration infrastructure.
- `builder.Build()` constructs the web application from the builder's configuration and registered services.
- `app.MapGet(...)` registers an endpoint that handles HTTP GET requests matching the root path `/`.
- `app.Run()` starts the application and blocks while the host is running, processing incoming requests until shutdown.

The order matters. The builder is configured before the application is built. The application's request handling is then configured before `Run()` starts the server.

This minimal hosting model places startup configuration in one file. As an application grows, you can move endpoint definitions and service-registration logic into separate files and extension methods without changing the underlying startup sequence.

### 3. The `WebApplication` builder

The builder is the configuration stage of application startup. It allows you to configure services and other aspects of the application before constructing the running application.

For example:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register application services here.

var app = builder.Build();

// Configure request handling here.

app.Run();
```

The distinction between these stages is important.

**Before `Build()`:** Register services and configure the application.

**After `Build()`:** Configure middleware and endpoints on the application.

**At `Run()`:** Start serving requests.

The builder exposes a service collection through `builder.Services`. Services are objects or components that the application can request through dependency injection. Although dependency injection was introduced in the .NET Fundamentals area, ASP.NET Core uses it extensively for request handlers, middleware, and application services.

For example, a web application can register a service before building:

```csharp
builder.Services.AddSingleton<TimeProvider>(
    TimeProvider.System);
```

An endpoint can then request that service:

```csharp
app.MapGet("/time", (TimeProvider timeProvider) =>
    timeProvider.GetUtcNow().ToString("O"));
```

The framework resolves the `TimeProvider` parameter from the service container when invoking the endpoint. This is one way ASP.NET Core connects application infrastructure with request handling.

The singleton registration is appropriate for `TimeProvider.System` because it represents a shared system clock provider. Service lifetimes and more advanced registration patterns belong in the Dependency Injection in Web Apps module.

### 4. The application and the HTTP server

An ASP.NET Core application needs an HTTP server to accept network connections and expose requests to the application pipeline.

The default modern hosting setup uses Kestrel, a cross-platform web server included with ASP.NET Core. When you run a local web application, Kestrel listens on one or more configured addresses and ports.

For example, the application might be available at:

```text
http://localhost:5000
```

Here:

- `http` is the protocol.
- `localhost` identifies the local machine.
- `5000` is the port.

The actual address and port depend on the application's configuration and launch settings. Do not assume every application uses port `5000`.

When a client connects, the server receives the HTTP request and passes it into the ASP.NET Core request-processing pipeline. The application processes the request and produces a response, which the server sends back to the client.

In a typical development setup, the application is accessed directly through Kestrel. In a production deployment, it may also sit behind a reverse proxy or load balancer. The proxy can handle concerns such as TLS termination, routing, or forwarding requests, while ASP.NET Core continues to process the requests that reach the application.

### 5. The request-processing pipeline

The request-processing pipeline is the sequence of components that process incoming HTTP requests.

ASP.NET Core uses middleware to implement this pipeline. Each middleware component can inspect or modify a request, pass control to the next component, and inspect or modify the response as it travels back.

Conceptually, a request might follow this path:

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
HTTP Response
```

The response generally travels back through middleware that called the next component:

```text
Middleware A: before next
    Middleware B: before next
        Endpoint executes
    Middleware B: after next
Middleware A: after next
```

This pattern allows middleware to handle concerns that apply to many requests, such as logging, error handling, authentication, and response modification.

The exact middleware components and their order depend on the application. The important principle is that requests move through the pipeline in the order configured, and a component can prevent later components from running.

### 6. Writing middleware

Middleware is a component that participates in processing HTTP requests and responses. ASP.NET Core provides built-in middleware and supports custom middleware.

A simple middleware component can be registered with `app.Use`:

```csharp
app.Use(async (context, next) =>
{
    Console.WriteLine(
        $"Request: {context.Request.Method} {context.Request.Path}");

    await next(context);

    Console.WriteLine(
        $"Response status: {context.Response.StatusCode}");
});
```

The `context` parameter represents the current HTTP request and response. It provides access to request details, response headers, the response body, and other request-specific information.

The `next` delegate represents the next component in the pipeline. Calling `await next(context)` passes control to the rest of the pipeline and waits for it to finish.

This middleware logs the request before passing it along, then logs the resulting response status afterward.

If middleware does not call `next`, it can short-circuit the pipeline. For example:

```csharp
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/blocked")
    {
        context.Response.StatusCode =
            StatusCodes.Status403Forbidden;

        await context.Response.WriteAsync("Request blocked.");
        return;
    }

    await next(context);
});
```

Requests to `/blocked` receive a `403 Forbidden` response without reaching later middleware or the endpoint. Other requests continue through the pipeline.

This example demonstrates short-circuiting. It is not a substitute for a complete authorization system; real authorization should use the appropriate ASP.NET Core authentication and authorization facilities.

Middleware can be useful when behavior needs to apply across multiple endpoints. If behavior belongs only to one particular route, implementing it directly in the endpoint may be simpler.

### 7. Middleware ordering

Middleware order affects how an application behaves. Each component sees the request at the point where it enters that component, and downstream components run only if the preceding middleware passes control forward.

Consider two middleware components:

```csharp
app.Use(async (context, next) =>
{
    Console.WriteLine("First: before");
    await next(context);
    Console.WriteLine("First: after");
});

app.Use(async (context, next) =>
{
    Console.WriteLine("Second: before");
    await next(context);
    Console.WriteLine("Second: after");
});

app.MapGet("/", () => "Hello!");
```

When a request reaches `/`, the expected console sequence is:

```text
First: before
Second: before
Second: after
First: after
```

The first middleware executes before the second. Once the endpoint finishes, control unwinds in reverse order.

If the second middleware short-circuits the request, the endpoint does not execute. The first middleware still resumes after `await next(context)` completes, because its downstream pipeline has completed.

This behavior is fundamental to request processing. For example, exception-handling middleware must be positioned so that it can catch exceptions thrown by the components it is intended to handle.

Some middleware combinations also have ordering requirements. Authentication generally needs to run before authorization so that authorization can evaluate the authenticated user. Other middleware, such as static-file handling, may intentionally short-circuit requests before they reach later components.

The Middleware & Request Pipeline module will explore these rules in greater detail.

### 8. Endpoints and request handling

An endpoint represents a destination that can handle a request. In an ASP.NET Core application, routing identifies the appropriate endpoint based on request information such as the URL path and HTTP method.

A minimal API endpoint can be defined using `MapGet`:

```csharp
app.MapGet("/", () => "Home");
app.MapGet("/status", () => "Application is running");
```

The first endpoint handles GET requests to `/`. The second handles GET requests to `/status`.

The endpoint delegate provides the application-specific behavior. For example, it can calculate a result, retrieve data from a service, or return a response.

If a request does not match any configured endpoint, the application typically returns a `404 Not Found` response, unless other middleware changes that behavior.

Routing and endpoint matching are covered in the Routing & Endpoints module. Minimal APIs and request parameter binding are covered in the Minimal APIs module. For now, focus on the overall relationship:

- Middleware processes requests across the pipeline.
- Routing determines which endpoint matches a request.
- The endpoint performs the operation associated with that request.
- ASP.NET Core writes the resulting response.

### 9. HTTP responses and status codes

An HTTP response communicates the result of a request. It contains a status code, headers, and potentially a body.

Common status codes include:

- `200 OK`: the request succeeded.
- `201 Created`: a resource was created.
- `204 No Content`: the request succeeded and there is no response body.
- `400 Bad Request`: the request was invalid.
- `401 Unauthorized`: authentication is required or failed.
- `403 Forbidden`: the client is not permitted to perform the operation.
- `404 Not Found`: the requested resource or endpoint was not found.
- `500 Internal Server Error`: an unexpected server-side failure occurred.

An endpoint can return a status code explicitly:

```csharp
app.MapGet("/health", () =>
    Results.Ok(new { status = "healthy" }));

app.MapGet("/missing", () =>
    Results.NotFound());
```

The first endpoint returns a successful response containing a JSON object. The second returns a `404 Not Found` response.

The response status should communicate the actual outcome. A failed operation should not return `200 OK` merely because the server successfully processed the request.

The HTTP & REST APIs module will cover status codes, HTTP methods, request bodies, response bodies, and REST conventions in greater detail.

### 10. Development and production environments

ASP.NET Core applications can behave differently depending on the environment in which they run.

The environment name is commonly set through configuration, often using the `ASPNETCORE_ENVIRONMENT` environment variable. Common names include `Development`, `Staging`, and `Production`.

For example, in a Linux shell you can set the environment for a single command:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

The application can inspect the active environment:

```csharp
if (app.Environment.IsDevelopment())
{
    Console.WriteLine("Development environment");
}
```

Development environments can enable more detailed diagnostics and developer-focused error pages. Production environments should avoid exposing internal exception details or sensitive information to clients.

Environment-specific behavior should be intentional. Do not place passwords, API keys, or other secrets directly in source code or commit them to a repository. Configuration providers and secret-management options will be covered in Configuration & Environments in Web Apps.

### 11. Creating and running a minimal web application

You can create an ASP.NET Core application with the .NET CLI. From a suitable learning directory, run:

```bash
dotnet new web -n 01-AspNetCoreFundamentals
```

This creates a minimal ASP.NET Core web project. The generated `.csproj` defines the project configuration, and `Program.cs` contains the application startup code.

Open the generated `Program.cs` and replace its contents with:

```csharp
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "Hello from ASP.NET Core!");

app.MapGet("/status", () =>
    Results.Ok(new { status = "running" }));

app.Run();
```

Run the application:

```bash
dotnet run --project 01-AspNetCoreFundamentals
```

The terminal displays the addresses on which the application is listening. Use the actual address printed by your application rather than assuming a particular port.

In a separate terminal, send an HTTP request to the address shown. For example, if the application reports `http://localhost:5000`:

```bash
curl -i http://localhost:5000/
```

The `-i` option displays the response headers as well as the body. You should receive a successful HTTP response containing the text `Hello from ASP.NET Core!`.

Check the second endpoint:

```bash
curl -i http://localhost:5000/status
```

This request should return a successful response containing a JSON object similar to:

```json
{
  "status": "running"
}
```

The exact headers and JSON formatting may vary. The important result is that the application accepts HTTP requests, selects the appropriate endpoint, and returns the expected response.

To stop the server, return to the terminal where it is running and press `Ctrl+C`.

### 12. How the pieces fit together

The startup process and request-processing process are related, but they happen at different times.

During startup, the application creates its builder, registers services, builds the application, configures middleware and endpoints, and starts the server.

During request processing, the server receives an HTTP request and passes it into the configured pipeline. Middleware can inspect or modify the request, routing selects a matching endpoint, and the endpoint performs its work. The response then travels back through the pipeline and is sent to the client.

The distinction matters because startup code configures the application, while request-handling code runs in response to incoming requests.

For the Boids simulation, these concepts establish the foundation for hosting web endpoints that may eventually expose simulation data or accept commands. The Blazor application may be able to perform much of the simulation logic on the client, so a custom backend should be added only when the application's requirements call for it.

## Challenge

Build a minimal ASP.NET Core web application that demonstrates the hosting model, the request-processing pipeline, and endpoint handling.

Your application must satisfy all of the following requirements:

1. **Create the web project:** Create an ASP.NET Core web project using the `web` template. Keep the project inside the `01-AspNetCoreFundamentals` learning area.
2. **Configure the application:** Use `WebApplication.CreateBuilder(args)`, build the application, configure request handling, and call `Run()` in the correct order.
3. **Create a root endpoint:** Implement a GET endpoint at `/` that returns a clear text response.
4. **Create a status endpoint:** Implement a GET endpoint at `/status` that returns a successful HTTP response containing a JSON object with a status property.
5. **Add request-logging middleware:** Log the HTTP method and path before passing the request to the next component. After the downstream pipeline completes, log the response status code.
6. **Demonstrate middleware ordering:** Add a second middleware component that logs before and after the next component. Observe the order in which the messages appear.
7. **Demonstrate short-circuiting:** Add a middleware condition for a specific path, such as `/blocked`, that returns `403 Forbidden` without calling the next component. Confirm that the request does not reach the endpoint.
8. **Handle an unmatched route:** Request a path that does not have an endpoint and confirm that the application returns `404 Not Found`.
9. **Run and inspect requests:** Run the application and use `curl -i` to inspect the status code, headers, and body of the root, status, blocked, and unmatched requests.
10. **Keep the code organized:** Keep the startup and pipeline configuration in `Program.cs`. Do not add unnecessary classes or abstractions for this exercise.

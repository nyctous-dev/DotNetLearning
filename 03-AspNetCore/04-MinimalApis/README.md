# Minimal APIs

## Micro Goal

Learn to define HTTP endpoints in ASP.NET Core using Minimal APIs, bind incoming request data to C# parameters, return appropriate HTTP responses, and organize endpoint logic so it remains maintainable as an application grows.

## Concepts Covered

- Minimal API structure and endpoint definitions
- `WebApplication` and endpoint registration
- Mapping endpoints with `MapGet`, `MapPost`, `MapPut`, `MapPatch`, and `MapDelete`
- Route parameters and query parameters
- Binding request data to handler parameters
- Request bodies and JSON deserialization
- Returning values and explicit HTTP results
- `IResult`, `Results`, and `TypedResults`
- HTTP status codes and response semantics
- Handling missing resources and invalid requests
- Grouping endpoints with `MapGroup`
- Extracting endpoint logic into methods
- Separating HTTP concerns from application logic
- Testing endpoints with HTTP requests

## Completion Criteria

- [ ] Create and run an ASP.NET Core Minimal API project.
- [ ] Define GET, POST, PUT, PATCH, and DELETE endpoints.
- [ ] Bind route parameters, query parameters, and request bodies.
- [ ] Return appropriate response data and HTTP status codes.
- [ ] Use `Results` or `TypedResults` to return explicit HTTP results.
- [ ] Handle missing resources and invalid input without returning misleading success responses.
- [ ] Use `MapGroup` to organize related endpoints under a common route prefix.
- [ ] Extract application logic from endpoint handlers when appropriate.
- [ ] Test endpoints using HTTP requests and verify both response bodies and status codes.
- [ ] Build a small API that manages Boid records in memory and follows consistent endpoint conventions.

## Lecture

### 1. What Minimal APIs are

ASP.NET Core Minimal APIs provide a way to build HTTP APIs with relatively little framework boilerplate. Instead of defining controller classes and action methods, you map HTTP methods and routes directly to handler delegates.

A Minimal API is still an ASP.NET Core web application. It uses the ASP.NET Core hosting model, HTTP request pipeline, dependency injection, configuration, and logging. "Minimal" describes the endpoint programming model, not a separate runtime.

Create a web API project from the `03-aspnet-core` learning area:

```bash
dotnet new web -n 04-MinimalApis
```

If the directory already exists as a learning area containing this README, create the project in a separate child directory or use the project name and path you choose for this module. Avoid creating a project on top of unrelated existing files.

A basic `Program.cs` looks like this:

```csharp
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "API is running.");

app.Run();
```

`WebApplication.CreateBuilder(args)` initializes the application's builder and default services and configuration. `builder.Build()` constructs the web application. `MapGet` registers an endpoint for HTTP GET requests at `/`. `app.Run()` starts the application and waits for incoming requests.

Run the project with:

```bash
dotnet run
```

The console output identifies the address or addresses on which the application is listening. Use that address when sending HTTP requests. Do not assume a particular port, because the development environment may assign a different one.

### 2. Mapping HTTP methods to endpoints

An endpoint combines a route pattern with behavior that handles matching requests. Minimal APIs provide mapping methods for the common HTTP verbs:

```csharp
app.MapGet("/boids", () => "List boids");
app.MapPost("/boids", () => "Create a boid");
app.MapPut("/boids/{id:int}", (int id) => $"Replace boid {id}");
app.MapPatch("/boids/{id:int}", (int id) => $"Update boid {id}");
app.MapDelete("/boids/{id:int}", (int id) => $"Delete boid {id}");
```

These are demonstrations of endpoint mapping, not a complete data API. They return text and do not yet store or modify records.

Each mapping method associates a route with a particular HTTP method:

- `MapGet` handles GET requests.
- `MapPost` handles POST requests.
- `MapPut` handles PUT requests.
- `MapPatch` handles PATCH requests.
- `MapDelete` handles DELETE requests.

HTTP method semantics matter. GET retrieves information and should not be used to change application state. POST commonly creates a resource or initiates an operation. PUT typically replaces a resource representation, PATCH applies a partial update, and DELETE removes a resource.

An endpoint must implement the semantics its API promises. Mapping a method to a route does not automatically provide persistence, validation, or correct resource-management behavior.

### 3. Route parameters

A route parameter captures a value from the URL path.

```csharp
app.MapGet("/boids/{id}", (int id) =>
{
    return $"Requested boid: {id}";
});
```

A request such as `GET /boids/12` supplies `12` as the `id` argument. ASP.NET Core binds the route value to the handler parameter.

Route constraints can restrict which values match a route:

```csharp
app.MapGet("/boids/{id:int}", (int id) =>
{
    return $"Requested boid: {id}";
});
```

The `:int` constraint requires the route segment to match an integer. A URL with a non-integer segment will not match this route, so the framework normally returns a 404 response if no other endpoint matches it.

Route constraints help disambiguate routes and restrict matching. They are not a substitute for all input validation. A route can match an integer that is negative or otherwise invalid for your application.

You can include several route parameters:

```csharp
app.MapGet("/simulations/{simulationId:int}/boids/{boidId:int}",
    (int simulationId, int boidId) =>
        $"Simulation {simulationId}, boid {boidId}");
```

Keep route names meaningful. A resource identifier belongs naturally in the route when the request refers to a specific resource.

### 4. Query parameters

Query parameters appear after `?` in a URL and are commonly used for filtering, sorting, and pagination.

For example:

```text
GET /boids?limit=10
```

A Minimal API handler can accept a query parameter:

```csharp
app.MapGet("/boids", (int? limit) =>
{
    int effectiveLimit = limit ?? 20;

    return $"Requested limit: {effectiveLimit}";
});
```

The nullable `int?` allows the parameter to be absent. The null-coalescing operator `??` supplies a default value when it is not provided.

Query parameters are useful when a request asks for a subset of a collection rather than a single resource identified by its path.

For a real API, define how the application handles values such as negative limits, zero limits, excessively large limits, or malformed input. A default value does not by itself establish a valid range.

### 5. Binding request data

Minimal APIs can bind handler parameters from several sources, including route values, query strings, request headers, services registered with dependency injection, and request bodies.

For example, consider an endpoint that receives coordinates:

```csharp
app.MapPost("/positions", (PositionRequest request) =>
{
    return Results.Ok(new
    {
        request.X,
        request.Y
    });
});

public record PositionRequest(double X, double Y);
```

A request body containing JSON such as:

```json
{
  "x": 10.0,
  "y": 20.0
}
```

can be deserialized into a `PositionRequest` object. The endpoint then returns a JSON response containing the supplied coordinates.

The `PositionRequest` type is a request DTO: a data transfer object that defines the shape of incoming data. It establishes the API's input contract without requiring the API to expose an internal domain object directly.

For a project with top-level statements, declare the record after the top-level statements, or place it in a separate `.cs` file. C# requires top-level statements to precede namespace and type declarations in the same file.

Binding and validation are different concerns. Successful deserialization means the framework was able to construct an object from the request data; it does not necessarily mean the values satisfy the application's rules. Explicitly validate important requirements, such as whether coordinates and speeds are finite and whether identifiers or limits are in an allowed range.

### 6. Returning responses

An endpoint can return a simple value:

```csharp
app.MapGet("/status", () => "Running");
```

ASP.NET Core converts the returned value into an HTTP response. Returning an object generally produces a JSON response using the framework's configured JSON serialization.

For APIs, explicit HTTP results make response semantics clearer:

```csharp
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy"
}));
```

The `Results` API provides common result factories, including:

- `Results.Ok(value)` — returns HTTP 200 with a response body.
- `Results.Created(uri, value)` — returns HTTP 201 and identifies the created resource.
- `Results.NoContent()` — returns HTTP 204 without a response body.
- `Results.BadRequest()` — returns HTTP 400 for a bad request.
- `Results.NotFound()` — returns HTTP 404 when a requested resource does not exist.
- `Results.Conflict()` — returns HTTP 409 for a conflict with the current resource state.

Choose a status code that accurately describes the outcome. Do not return HTTP 200 for every condition, especially when the requested resource does not exist or the input is invalid.

`IResult` is an interface implemented by HTTP result types. The `Results` factory methods provide convenient ways to create results. `TypedResults` offers strongly typed result objects, which can improve type information and endpoint metadata.

For example:

```csharp
app.MapGet("/boids/{id:int}", (int id) =>
{
    if (id <= 0)
    {
        return Results.BadRequest(
            new { error = "The ID must be positive." });
    }

    return Results.Ok(new { id });
});
```

In this example, the two branches return different result types. The `Results` factory methods are designed for this kind of endpoint. If you use `TypedResults` and need several possible response types, consider a suitable common return type or a typed union so the endpoint's return contract remains clear.

### 7. Resource-oriented endpoint design

A REST-style API commonly uses nouns in resource paths and HTTP methods to express the requested operation.

For a boid resource, a consistent set of routes might be:

| Method | Route | Purpose |
|---|---|---|
| GET | `/boids` | List boids |
| GET | `/boids/{id}` | Retrieve one boid |
| POST | `/boids` | Create a boid |
| PUT | `/boids/{id}` | Replace a boid |
| PATCH | `/boids/{id}` | Partially update a boid |
| DELETE | `/boids/{id}` | Delete a boid |

These routes establish a predictable contract. A client can list records, retrieve one record, create one, update it, or delete it.

A collection endpoint may also support query parameters:

```text
GET /boids?limit=20
```

Avoid encoding every action as a separate verb in the route. Resource-oriented paths and HTTP methods usually communicate the operation more consistently.

REST is an architectural style, not a requirement to use one specific route format. Choose conventions that suit the resource and document behavior clearly.

### 8. Implementing an in-memory API

Before introducing a database, you can use a collection to demonstrate resource operations. An in-memory store is useful for learning because it makes the request and response behavior visible without requiring persistence infrastructure.

For example, a simplified model might be:

```csharp
public record BoidResponse(
    int Id,
    double X,
    double Y,
    double Speed);
```

An application can maintain a `List<BoidResponse>` and implement endpoints that read and modify it. A counter can assign identifiers when records are created.

This approach is appropriate for a small learning exercise, but it has limitations:

- Data is lost when the application restarts.
- Multiple application instances do not share the same collection.
- Concurrent requests may need synchronization if they can mutate shared state.
- A simple list lookup is not a substitute for a database when the dataset grows.

Keep the limitation explicit. An in-memory store is a demonstration of HTTP and endpoint behavior, not durable production storage.

### 9. Handling missing resources and invalid input

An endpoint should define what happens when a requested resource does not exist or a request violates the API's rules.

For example, a GET endpoint should return 404 when the requested ID is not found. A creation endpoint should reject invalid values rather than silently accepting them.

A useful endpoint behavior table might be:

| Situation | Typical response |
|---|---|
| Resource found | 200 OK |
| Resource created | 201 Created |
| Successful update with no response body | 204 No Content |
| Invalid request data | 400 Bad Request |
| Resource not found | 404 Not Found |
| Conflict with current state | 409 Conflict |

These are common choices, not universal rules for every possible API. Use the response that best represents the endpoint's documented contract.

Do not catch every exception and convert it to a 400 response. A malformed request may be a client error, while an unexpected database or programming failure is a server error. Error handling should preserve that distinction and avoid exposing internal exception details to clients.

### 10. Organizing endpoints with `MapGroup`

As an API grows, registering every endpoint directly in `Program.cs` can become difficult to navigate. `MapGroup` creates a route group with a shared prefix and can apply common conventions or endpoint metadata.

```csharp
var boids = app.MapGroup("/boids");

boids.MapGet("/", () => Results.Ok("List boids"));

boids.MapGet("/{id:int}", (int id) =>
    Results.Ok(new { id }));
```

These endpoints are exposed under `/boids` and `/boids/{id}` respectively.

Groups are useful for organizing related endpoints and applying shared authorization or other conventions when needed. They do not automatically create a separate service or controller. They organize endpoint registration within the Minimal API model.

For larger applications, you can move endpoint registration into extension methods or dedicated modules. Keep this refactoring proportional to the size of the application; a tiny exercise does not need a large architectural framework.

### 11. Separating HTTP handling from application logic

An endpoint handler should coordinate the request and response, while reusable business rules should live in methods or services.

For example, a speed calculation does not need to know whether its input arrived over HTTP:

```csharp
public static class BoidCalculations
{
    public static double CalculateSpeed(
        double baseSpeed,
        double multiplier)
    {
        if (!double.IsFinite(baseSpeed) ||
            !double.IsFinite(multiplier) ||
            baseSpeed < 0 ||
            multiplier < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(baseSpeed),
                "Speed values must be finite and non-negative.");
        }

        return baseSpeed * multiplier;
    }
}
```

An endpoint can call this method and translate the result into an HTTP response. The calculation can also be tested directly without starting a web server.

As the API grows, use services to hold application logic that needs reuse, state management, or external dependencies. Dependency injection is already covered in .NET Fundamentals; here the focus is how endpoint handlers use those services in a web request.

Avoid putting database access, validation rules, calculations, and response formatting into one large handler. Separating responsibilities makes the API easier to test and maintain.

### 12. Testing endpoints

A Minimal API should be tested at the HTTP boundary as well as through unit tests of its underlying logic.

During development, you can send requests with tools such as `curl`. For example, once the application is running at the appropriate local address:

```bash
curl -i http://localhost:5000/health
```

Replace the port with the actual listening port shown by your application. The `-i` option displays the HTTP status and response headers along with the body.

For a POST endpoint that accepts JSON:

```bash
curl -i \
  -X POST \
  -H "Content-Type: application/json" \
  -d '{"x":10.0,"y":20.0}' \
  http://localhost:5000/positions
```

Replace the address and route with those exposed by your application.

Check more than the response text. Verify the status code, headers where relevant, and JSON body. Test valid input, invalid input, missing resources, and unsupported routes or methods.

For automated integration tests, ASP.NET Core supports hosting the application in a test environment and issuing HTTP requests without relying on a manually started development server. This is a later step; the immediate goal is to understand the HTTP contract and verify it consistently.

## Challenge

Build an ASP.NET Core Minimal API that manages Boid records in memory. The API should demonstrate endpoint mapping, parameter binding, HTTP results, and simple separation between HTTP handling and application logic.

Your program must satisfy all of the following requirements:

1. **Create a web project:** Use an ASP.NET Core web project and configure it to run locally.
2. **Define a Boid model:** Include an integer ID, X and Y coordinates, and a speed. Use a request DTO for incoming creation data rather than accepting the response model directly.
3. **List resources:** Implement `GET /boids` to return all current boids.
4. **Retrieve one resource:** Implement `GET /boids/{id:int}`. Return 404 if the requested boid does not exist.
5. **Create a resource:** Implement `POST /boids`. Validate the incoming data, assign a new ID, store the boid, and return 201 Created with the new resource and its location.
6. **Replace a resource:** Implement `PUT /boids/{id:int}` to replace an existing boid's editable data. Return 404 when the ID does not exist.
7. **Delete a resource:** Implement `DELETE /boids/{id:int}`. Return 204 on successful deletion and 404 when no such resource exists.
8. **Validate inputs:** Reject negative speeds and non-finite numeric values. Return a suitable client-error response for invalid data.
9. **Use appropriate results:** Return meaningful HTTP status codes and JSON responses instead of returning success for every request.
10. **Organize endpoints:** Use `MapGroup("/boids")` to group the resource endpoints.
11. **Separate logic:** Put at least one reusable business rule in a separate method or class rather than embedding every operation in endpoint lambdas.
12. **Test manually:** Use `curl` or an equivalent HTTP client to list boids, create a boid, retrieve it, replace it, delete it, and verify the not-found responses.
13. **Keep scope focused:** Use in-memory storage for this exercise. Do not add a database, authentication, or unrelated application features.


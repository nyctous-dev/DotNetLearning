# Routing & Endpoints

## Micro Goal

Understand how ASP.NET Core matches incoming HTTP requests to endpoints using route templates, HTTP methods, route parameters, and constraints, and learn how to define predictable URL patterns for web applications.

## Concepts Covered

- Routing in ASP.NET Core
- Endpoints and endpoint execution
- Route templates and URL patterns
- Route parameters and route values
- Required and optional parameters
- Route constraints
- HTTP method matching
- Attribute routing concepts
- Route groups and shared prefixes
- Query string parameters versus route parameters
- Endpoint matching and ambiguous routes
- Returning appropriate HTTP results
- Route naming and URL generation
- Organizing related endpoints
- Testing routes with HTTP requests

## Completion Criteria

- [ ] Explain how routing connects an incoming HTTP request to an endpoint.
- [ ] Define endpoints for different HTTP methods and URL patterns.
- [ ] Extract values from route parameters.
- [ ] Distinguish route parameters from query string parameters.
- [ ] Use optional route parameters and route constraints correctly.
- [ ] Explain why the HTTP method is part of endpoint matching.
- [ ] Organize related endpoints with route groups.
- [ ] Return appropriate status codes for successful and unsuccessful requests.
- [ ] Recognize route conflicts and avoid ambiguous endpoint definitions.
- [ ] Test endpoint behavior using HTTP requests.
- [ ] Build a small set of well-defined endpoints for a Boids simulation API.

## Lecture

### 1. What routing does

A web application receives HTTP requests. Each request contains information such as the HTTP method, URL path, headers, and potentially a body.

Routing determines which endpoint should handle a request based on its characteristics. In ASP.NET Core, an endpoint typically combines a route pattern with information about how the request should be handled.

For example, a Boids simulation API might expose these URLs:

```text
GET    /api/boids
GET    /api/boids/42
POST   /api/boids
DELETE /api/boids/42
```

These routes represent different operations:

- `GET /api/boids` retrieves the collection of boids.
- `GET /api/boids/42` retrieves a particular boid.
- `POST /api/boids` creates a boid.
- `DELETE /api/boids/42` deletes the boid with identifier `42`.

Routing helps distinguish these requests and select the appropriate handler. It does not automatically implement the business logic, access a database, or guarantee that a requested resource exists. The endpoint handler must perform the required work and return an appropriate result.

### 2. Endpoints in ASP.NET Core

An endpoint is a selectable destination in the request-processing system. In a Minimal API application, endpoints are commonly defined with methods such as `MapGet`, `MapPost`, `MapPut`, and `MapDelete`.

For example:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Boids API");

app.MapGet("/api/status", () =>
{
    return Results.Ok(new { status = "running" });
});

app.Run();
```

`MapGet` registers an endpoint for HTTP GET requests that match the specified route pattern.

The first endpoint returns a simple string. The second returns an HTTP 200 response with a JSON-compatible object describing the application status.

The distinction between registering an endpoint and executing it is important. Calling `MapGet` configures the application. The handler executes later when a matching request arrives.

`WebApplication.CreateBuilder(args)` and `builder.Build()` are part of the standard ASP.NET Core application setup. The focus in this module is how the endpoints registered on `app` match requests and produce results, rather than revisiting the entire hosting model.

### 3. Route templates and URL paths

A route template defines the path pattern that an endpoint matches.

A literal route matches a specific path:

```csharp
app.MapGet("/api/boids", () => "All boids");
```

This endpoint matches `/api/boids`. It does not automatically match `/api/boids/42`.

A route parameter represents a variable portion of the path:

```csharp
app.MapGet("/api/boids/{id}", (int id) =>
{
    return Results.Ok(new { boidId = id });
});
```

A request to `/api/boids/42` binds `42` to the `id` parameter. The handler then returns that value in its response.

Route parameter names are written inside braces. The parameter name in the handler is used for binding, and a simple route parameter can bind directly to a compatible .NET type.

A route template describes which URL paths can match. It does not require that a corresponding database record exists. For example, `/api/boids/999` can match the route even if boid `999` has never been created. The handler must check whether that resource exists.

### 4. Route parameters

Route parameters are useful when a value identifies a particular resource or is an essential part of the path.

Consider these routes:

```text
/api/boids/42
/api/simulations/7
/api/simulations/7/boids/42
```

The values `42` and `7` can be represented by route parameters:

```csharp
app.MapGet("/api/simulations/{simulationId}/boids/{boidId}",
    (int simulationId, int boidId) =>
    {
        return Results.Ok(new
        {
            simulationId,
            boidId
        });
    });
```

A request to `/api/simulations/7/boids/42` binds `simulationId` to `7` and `boidId` to `42`.

Use descriptive names such as `simulationId` rather than vague names such as `x` or `value`. Clear names make route definitions easier to understand and help keep handler parameters meaningful.

A route parameter is generally required unless the template explicitly makes it optional. If a required segment is missing, the route does not match.

### 5. Optional route parameters

Sometimes a route segment should be optional. ASP.NET Core supports optional route parameters with a trailing question mark:

```csharp
app.MapGet("/api/boids/{id?}", (int? id) =>
{
    return Results.Ok(new
    {
        requestedId = id
    });
});
```

The route can match both `/api/boids` and `/api/boids/42`. The nullable `int?` allows the handler to represent the case where no identifier was supplied.

Optional route parameters should be used deliberately. If collection retrieval and individual-resource retrieval have different semantics, separate endpoints are often clearer:

```csharp
app.MapGet("/api/boids", () => Results.Ok("All boids"));

app.MapGet("/api/boids/{id:int}", (int id) =>
{
    return Results.Ok(new { boidId = id });
});
```

This makes the collection operation distinct from the single-resource operation. The second route also constrains the identifier to an integer.

### 6. Route constraints

A route constraint restricts the values that can match a route parameter. Constraints are part of route matching, not a replacement for business validation.

For example:

```csharp
app.MapGet("/api/boids/{id:int}", (int id) =>
{
    return Results.Ok(new { boidId = id });
});
```

The `:int` constraint means that the route parameter must match an integer representation. A request to `/api/boids/42` can match, while `/api/boids/abc` does not match this endpoint.

Common constraints include:

- `:int` — integer values.
- `:long` — long integer values.
- `:guid` — GUID values.
- `:bool` — Boolean values.
- `:alpha` — alphabetic characters.
- `:min(1)` — numeric values with a minimum of `1`.
- `:max(100)` — numeric values with a maximum of `100`.
- `:length(5)` — strings of a specified length.
- `:minlength(2)` and `:maxlength(20)` — string length bounds.
- `:regex(...)` — values matching a regular expression.

For example:

```csharp
app.MapGet("/api/simulations/{id:guid}", (Guid id) =>
{
    return Results.Ok(new { simulationId = id });
});
```

A GUID constraint is useful when a resource identifier is represented by a GUID.

Constraints help routing choose the correct endpoint, but they should not be used to communicate every possible validation rule. A route constraint that rejects a request typically causes that route not to match. It does not necessarily produce a detailed validation response.

For example, `:int` checks the representation of an identifier, but it does not determine whether the identifier is positive, authorized, or associated with an existing resource. The handler or application logic must enforce those rules.

### 7. HTTP methods are part of endpoint matching

HTTP methods express the intended operation. Common methods include:

- `GET` — retrieve a resource or collection.
- `POST` — submit data, often to create a resource or initiate an operation.
- `PUT` — replace a resource or set its state according to the API's contract.
- `PATCH` — partially modify a resource.
- `DELETE` — remove a resource.

ASP.NET Core uses the HTTP method when selecting endpoints. You can register separate handlers for the same path with different methods:

```csharp
app.MapGet("/api/boids", () =>
{
    return Results.Ok("Retrieve boids");
});

app.MapPost("/api/boids", () =>
{
    return Results.Created("/api/boids/42",
        new { id = 42 });
});
```

A GET request and a POST request to `/api/boids` are different operations even though the path is identical.

Use the method that matches the operation's semantics. Do not use a GET request to perform an operation that changes server state. GET is expected to be safe and is commonly cached or automatically retried by clients and intermediaries.

If a path matches an endpoint for one method but the client sends another method, ASP.NET Core commonly responds with `405 Method Not Allowed` when it recognizes that the path exists for a different method. If no endpoint matches the request, the result is commonly `404 Not Found`.

The precise response can depend on the application's endpoint configuration and middleware, but these are the expected distinctions for ordinary API routing.

### 8. Route parameters versus query string parameters

Route parameters and query string parameters both carry values, but they serve different purposes.

A route parameter is part of the path:

```text
/api/boids/42
```

A query string follows a question mark:

```text
/api/boids?limit=20&active=true
```

Route parameters commonly identify the resource or path segment being addressed. Query string parameters commonly filter, sort, paginate, or otherwise adjust a request.

For example:

```csharp
app.MapGet("/api/boids", (int? limit, bool? active) =>
{
    return Results.Ok(new
    {
        limit,
        active
    });
});
```

A request to `/api/boids?limit=20&active=true` binds the query string values to the handler parameters.

By contrast:

```csharp
app.MapGet("/api/boids/{id:int}", (int id) =>
{
    return Results.Ok(new { boidId = id });
});
```

Here, the identifier is part of the path.

A practical rule is to use a route parameter for the identity or hierarchy of a resource and query parameters for optional selection or presentation criteria. This is a convention rather than an absolute law; choose URL structures that make the API's behavior predictable.

### 9. Returning appropriate results

Routing selects an endpoint, but the endpoint must still communicate the result of the operation. ASP.NET Core provides result helpers for common HTTP responses.

```csharp
app.MapGet("/api/boids/{id:int}", (int id) =>
{
    if (id <= 0)
    {
        return Results.BadRequest(
            new { error = "The identifier must be positive." });
    }

    if (id != 42)
    {
        return Results.NotFound();
    }

    return Results.Ok(new
    {
        id = 42,
        x = 10.0,
        y = 20.0,
        speed = 2.5
    });
});
```

This example distinguishes several outcomes:

- `Results.Ok(...)` returns HTTP 200 with a response body.
- `Results.BadRequest(...)` returns HTTP 400 for an invalid request.
- `Results.NotFound()` returns HTTP 404 when the requested resource is not found.

For a create operation, `Results.Created(...)` can return HTTP 201 and a location for the newly created resource. For an operation that succeeds without returning a body, `Results.NoContent()` returns HTTP 204.

Choose status codes based on the API contract. A request that is syntactically valid but refers to a nonexistent resource usually receives 404. A request that violates the endpoint's input requirements may receive 400. Authentication and authorization failures have their own status codes and should not be confused with route matching.

### 10. Route groups

Related endpoints often share a common prefix. Route groups allow you to define that prefix once and organize endpoints together.

```csharp
var boids = app.MapGroup("/api/boids");

boids.MapGet("/", () => Results.Ok("All boids"));

boids.MapGet("/{id:int}", (int id) =>
{
    return Results.Ok(new { boidId = id });
});

boids.MapDelete("/{id:int}", (int id) =>
{
    return Results.NoContent();
});
```

The group prefix is `/api/boids`. The routes registered within it add their own relative patterns.

This approach keeps related endpoints together and reduces repeated route prefixes. Route groups can also be used to apply shared endpoint conventions, authorization requirements, or metadata to related endpoints.

The group does not automatically implement storage, resource existence checks, or deletion logic. In this example, the delete endpoint returns a successful no-content response as a demonstration; a real implementation must actually perform the deletion and handle a missing resource appropriately.

When using route groups, be careful with trailing slashes and relative route templates. Keep the final URL structure consistent and verify the actual paths with HTTP requests.

### 11. Route conflicts and ambiguous matches

A route design should make endpoint selection predictable.

Consider these endpoints:

```csharp
app.MapGet("/api/boids/{value}", (string value) =>
{
    return Results.Ok(new { value });
});

app.MapGet("/api/boids/{name}", (string name) =>
{
    return Results.Ok(new { name });
});
```

Both templates have the same path structure. Renaming the parameter does not make the routes distinct. A request such as `/api/boids/example` cannot be distinguished by those parameter names alone, so the route definitions can produce an ambiguous match.

Use distinct literal segments, appropriate constraints, or a different URL design:

```csharp
app.MapGet("/api/boids/{id:int}", (int id) =>
{
    return Results.Ok(new { boidId = id });
});

app.MapGet("/api/boids/by-name/{name}", (string name) =>
{
    return Results.Ok(new { name });
});
```

Now the identifier route expects an integer, and the name route has a separate literal segment.

Avoid designing endpoints that depend on the order in which they are registered to resolve an otherwise ambiguous match. Prefer route templates that clearly express the intended operation.

### 12. Route names and URL generation

Route names allow an endpoint to be identified for URL generation. They are not the same as route parameter names or the visible URL path.

For example:

```csharp
app.MapGet("/api/boids/{id:int}", (int id) =>
{
    return Results.Ok(new { boidId = id });
})
.WithName("GetBoidById");
```

The route name `GetBoidById` can be used by ASP.NET Core's link-generation facilities to generate a URL for the endpoint, provided the required route values are supplied.

This is useful when one endpoint needs to refer to another resource. Instead of manually constructing a URL in multiple places, an application can generate a URL from the named route and its parameter values.

Route names should be unique when used for link generation. Naming routes is optional for simple applications, but it becomes useful as APIs grow and create operations need to return links to newly created resources.

### 13. Testing routes with HTTP requests

A route definition is not fully verified just because the application compiles. Send requests to the running application and inspect the status codes and response bodies.

Start the application from its project directory:

```bash
dotnet run
```

The console output indicates the local URL where the application is listening. Use that address as the base URL for your requests.

For example, if the application listens on `http://localhost:5000`, you can test a collection endpoint with:

```bash
curl -i http://localhost:5000/api/boids
```

The `-i` option includes the response headers, making it easier to inspect the HTTP status code.

Test a parameterized route:

```bash
curl -i http://localhost:5000/api/boids/42
```

Test a route constraint by sending a non-integer identifier:

```bash
curl -i http://localhost:5000/api/boids/abc
```

If the only matching endpoint requires `{id:int}`, the invalid identifier should not match that endpoint, and the usual result is 404 unless another endpoint handles the path.

Test a different HTTP method:

```bash
curl -i -X DELETE http://localhost:5000/api/boids/42
```

Compare the behavior with a GET request to the same path. A DELETE endpoint and a GET endpoint should perform different operations according to their contracts.

When testing, verify both the status code and the response body. A response with a 200 status code is not sufficient if the body contains the wrong resource or incorrect data.

## Challenge

Build a small ASP.NET Core Minimal API that exposes routes for a Boids simulation. The goal is to demonstrate routing and endpoint behavior, not to build a database-backed application.

Your API must satisfy all of the following requirements:

1. **Create a collection endpoint:** Implement `GET /api/boids` that returns a collection of at least three boids.
2. **Create a single-resource endpoint:** Implement `GET /api/boids/{id:int}` that returns the boid with the requested identifier or HTTP 404 if it does not exist.
3. **Validate identifiers:** Return HTTP 400 when a matched identifier is not positive. Ensure the route constraint and handler validation have distinct purposes.
4. **Create a query string endpoint:** Allow `GET /api/boids?active=true` to filter the collection using a query parameter. Also support the absence of the optional parameter.
5. **Create a POST endpoint:** Implement `POST /api/boids` that accepts a simple boid request and returns HTTP 201 with a location pointing to the created resource.
6. **Create a DELETE endpoint:** Implement `DELETE /api/boids/{id:int}` that removes an existing boid and returns HTTP 204. Return HTTP 404 when the boid does not exist.
7. **Use a route group:** Organize the boid endpoints under a shared `/api/boids` prefix.
8. **Avoid ambiguous routes:** Ensure every endpoint has a distinct, predictable matching pattern.
9. **Test the API:** Use `curl` or another HTTP client to test successful requests, missing resources, invalid identifiers, query string filtering, creation, and deletion.
10. **Verify HTTP behavior:** Check both status codes and response bodies where applicable. Do not rely only on the console output or successful compilation.

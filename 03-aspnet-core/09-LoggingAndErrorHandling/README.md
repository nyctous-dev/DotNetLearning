# Logging & Error Handling in Web Apps

## Micro Goal

Learn how to diagnose ASP.NET Core web applications using structured logging, handle exceptions centrally through middleware, return appropriate HTTP error responses, and distinguish expected client errors from unexpected server failures.

## Concepts Covered

- `ILogger<T>` and dependency-injected logging
- Log levels and structured logging
- Logging HTTP request and application events
- Exception handling middleware
- The ASP.NET Core developer exception page
- Production error handling with `UseExceptionHandler`
- HTTP status codes and error responses
- `ProblemDetails` and `IProblemDetailsService`
- Returning expected client errors versus handling unexpected exceptions
- Validation errors and client feedback
- Middleware ordering and exception boundaries
- Avoiding sensitive information in logs and responses
- Correlation and trace identifiers
- Testing error responses and logging behavior

## Completion Criteria

- [ ] Inject `ILogger<T>` into an ASP.NET Core service or endpoint handler.
- [ ] Use appropriate log levels for different events.
- [ ] Write structured log messages with named properties.
- [ ] Explain the difference between the developer exception page and production exception handling.
- [ ] Configure centralized exception handling with `UseExceptionHandler`.
- [ ] Return appropriate HTTP status codes for expected error conditions.
- [ ] Use `ProblemDetails` to represent API errors consistently.
- [ ] Avoid exposing exception details, stack traces, credentials, or other sensitive information to clients.
- [ ] Explain how middleware ordering affects exception handling.
- [ ] Include useful diagnostic context without logging unnecessary or sensitive data.
- [ ] Test both expected client errors and unexpected server errors.
- [ ] Demonstrate that an error response does not expose internal implementation details.

## Lecture

### 1. Why logging and error handling matter in web applications

A web application receives requests from clients and returns HTTP responses. Requests can fail for several reasons:

- A client submits invalid data.
- A requested resource does not exist.
- A user lacks permission to perform an operation.
- A database or external service becomes unavailable.
- Application code contains a bug that causes an unexpected exception.

These situations should not all be handled the same way.

A missing resource is often an expected condition that should produce an HTTP `404 Not Found` response. Invalid input may produce `400 Bad Request`. An unexpected exception usually indicates a server-side failure and should produce a suitable `500 Internal Server Error` response.

Logging serves a different purpose. It records information that helps developers and operators understand what happened, diagnose problems, and monitor application behavior.

The distinction is important:

- **HTTP responses** communicate an outcome to the client.
- **Logs** provide diagnostic information to the application operator.
- **Exceptions** represent failures in program execution or operations.
- **Error handling middleware** provides a centralized way to convert unhandled exceptions into HTTP responses.

A well-designed application gives clients useful, safe error information while preserving enough diagnostic context for developers to investigate failures.

### 2. Logging with `ILogger<T>`

ASP.NET Core uses the `Microsoft.Extensions.Logging` abstractions for application logging. The `ILogger<T>` interface provides a logger associated with a particular type.

ASP.NET Core's dependency injection system can supply a logger to an endpoint handler or service.

For example, a minimal API endpoint can accept a logger as a parameter:

```csharp
app.MapGet("/status", (ILogger<Program> logger) =>
{
    logger.LogInformation("Status endpoint was requested.");

    return Results.Ok(new { status = "healthy" });
});
```

The framework supplies the logger. You do not need to construct it manually.

For application logic, injecting a logger into a service is also common:

```csharp
public sealed class BoidService(ILogger<BoidService> logger)
{
    public void RecordMovement(int boidId)
    {
        logger.LogInformation(
            "Recorded movement for boid {BoidId}",
            boidId);
    }
}
```

This example uses a primary constructor, a C# feature that allows constructor parameters to be declared in the class declaration. The logger is available to the service's methods.

The generic type parameter identifies the category associated with the logger. For `ILogger<BoidService>`, the category typically corresponds to the fully qualified name of `BoidService`. Categories make it possible to configure logging levels for different areas of an application.

### 3. Log levels

Logging levels communicate the severity and purpose of a message.

| Level | Intended use |
|---|---|
| `Trace` | Very detailed diagnostic information, often useful only for deep investigation |
| `Debug` | Information useful during development and debugging |
| `Information` | Normal application events worth recording |
| `Warning` | Unexpected or undesirable conditions that do not necessarily prevent the operation from continuing |
| `Error` | Failures that prevent an operation from completing successfully |
| `Critical` | Severe failures that may require immediate attention |
| `None` | Disables logging at the configured level |

Choose levels according to the event, not merely according to whether the code uses a `try`/`catch` block.

For example, a normal successful request might be logged at `Information`. A temporary condition that causes an operation to be retried might be a `Warning`. An unexpected exception that prevents a request from completing might be an `Error`.

Do not log every normal event as an error. Doing so makes real failures harder to identify.

### 4. Structured logging

Structured logging records named properties alongside a message. Instead of embedding every value into a formatted string, use message templates with named placeholders.

Prefer this:

```csharp
logger.LogInformation(
    "Boid {BoidId} moved to ({X}, {Y})",
    boidId,
    x,
    y);
```

Instead of this:

```csharp
logger.LogInformation(
    $"Boid {boidId} moved to ({x}, {y})");
```

The first example allows logging providers to preserve `BoidId`, `X`, and `Y` as structured properties. This makes the data easier to filter, query, and analyze.

The second example creates a formatted string before passing it to the logger. The values are included in the message text, but they are not represented as separate named properties by that call.

Use stable property names and avoid adding high-cardinality or sensitive values unnecessarily. For example, a request ID can be useful for correlating events, but a password, access token, or full payment-card number should never be logged.

### 5. Logging exceptions

When an operation fails with an exception, the logger can record the exception object:

```csharp
try
{
    await ProcessRequestAsync();
}
catch (Exception exception)
{
    logger.LogError(
        exception,
        "Failed to process the request.");

    throw;
}
```

Passing the exception object preserves diagnostic information such as the exception type and stack trace for logging providers that support it.

The `throw;` statement rethrows the current exception while preserving its original stack trace. In this example, the exception is logged and then propagated to an outer handler.

However, do not catch exceptions only to log and rethrow them at every layer. If multiple layers log the same exception, the result can be duplicated and confusing. Prefer to log an exception at the layer that can meaningfully handle it or at the centralized boundary that records the final failure.

Catch exceptions when you can take a useful action, translate the failure into an appropriate application result, or add meaningful context. Otherwise, let the exception propagate to the configured exception-handling middleware.

### 6. The ASP.NET Core request pipeline

ASP.NET Core processes HTTP requests through a sequence of middleware components. Each middleware can perform work before and after the next component, modify the request or response, or stop the pipeline.

A simplified request flow is:

```text
HTTP Request
    |
    v
Exception Handling Middleware
    |
    v
Routing
    |
    v
Authentication / Authorization
    |
    v
Endpoint Handler
    |
    v
HTTP Response
```

This is an illustrative sequence, not a universal ordering for every application. The required order depends on which middleware the application uses and what each component needs to observe.

Exception handling middleware is particularly important because it can catch exceptions thrown by components that execute later in the pipeline.

If an exception occurs in a downstream endpoint, exception handling middleware can log the failure and generate an HTTP error response, provided the response has not already started and the exception is within its handling boundary.

If middleware is placed outside the exception handler in the pipeline, exceptions thrown by that outer middleware will not be caught by the inner handler. Therefore, exception handling middleware is normally placed early in the pipeline.

### 7. Development errors versus production errors

During development, the developer exception page provides detailed information about unhandled exceptions. It can include stack traces and other debugging details.

This information is useful when developing an application locally, but it must not be exposed to ordinary clients in production.

A common configuration is:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler();
}
```

`UseDeveloperExceptionPage` provides detailed development diagnostics. `UseExceptionHandler` provides centralized handling for unexpected exceptions in environments where detailed diagnostics should not be exposed.

The developer exception page is commonly enabled by the development template, so inspect the generated `Program.cs` before adding another copy.

In production, an error response should not reveal:

- Stack traces or internal source paths.
- Database connection strings.
- Access tokens or credentials.
- Internal implementation details.
- Sensitive personal or business data.

A client generally needs to know that the request failed and, when appropriate, how to correct the request or identify the failure. Internal details belong in protected diagnostic systems.

### 8. Centralized exception handling

Without centralized handling, individual endpoints may end up repeating `try`/`catch` blocks and constructing error responses inconsistently.

ASP.NET Core provides `UseExceptionHandler` to handle unhandled exceptions at a shared pipeline boundary.

For APIs, an application can configure an exception handler that returns a consistent error response:

```csharp
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
        };

        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

app.MapGet("/failure", () =>
{
    throw new InvalidOperationException(
        "Internal diagnostic details.");
});

app.Run();
```

This example demonstrates a simple centralized handler. The exception is caught by the exception-handling middleware, and the client receives a generic server-error response rather than the exception message.

The `errorApp` branch is a separate middleware pipeline used to generate the error response. In a real application, the handler can use `IExceptionHandlerFeature` to access the original exception for controlled diagnostic logging.

For example:

```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature =
            context.Features.Get<IExceptionHandlerFeature>();

        var logger = context.RequestServices
            .GetRequiredService<ILogger<Program>>();

        if (exceptionFeature is not null)
        {
            logger.LogError(
                exceptionFeature.Error,
                "Unhandled exception while processing a request.");
        }

        context.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred."
            });
    });
});
```

This variation logs the underlying exception for diagnostics while returning a generic message to the client.

A production application should also consider what happens if the response has already started, whether the error response can be serialized, and how failures in the error handler itself are managed. Centralized handling reduces duplication, but it cannot guarantee that every possible failure can be converted into a new response.

### 9. Expected errors versus unexpected exceptions

Not every unsuccessful request should throw an exception.

For example, a client asking for a nonexistent boid is an expected application condition. The endpoint can return `404 Not Found` directly:

```csharp
app.MapGet("/boids/{id:int}", (int id) =>
{
    if (id <= 0)
    {
        return Results.BadRequest(
            new { error = "The boid ID must be positive." });
    }

    return id == 1
        ? Results.Ok(new { id, speed = 2.0 })
        : Results.NotFound();
});
```

This endpoint demonstrates several distinct outcomes:

- A nonpositive ID produces `400 Bad Request`.
- The known ID produces `200 OK`.
- An unknown positive ID produces `404 Not Found`.

These are normal application outcomes, so they do not need to be represented as unhandled exceptions.

Use exceptions for exceptional failures and explicit results for expected branches. This makes application behavior easier to understand and avoids using exceptions as routine control flow.

A database outage or an unexpected null dereference is different from a record simply not existing. Such failures may require exception handling and a server-error response.

### 10. HTTP status codes and error responses

HTTP status codes communicate the broad result of a request.

| Status | Meaning | Typical API use |
|---|---|---|
| `200 OK` | Request succeeded | Return a retrieved resource or successful result |
| `201 Created` | A resource was created | Confirm creation, often with a location for the new resource |
| `204 No Content` | Request succeeded without a response body | Successful deletion or update when no body is needed |
| `400 Bad Request` | Request is invalid | Invalid values or malformed input |
| `401 Unauthorized` | Authentication is required or invalid | Missing or invalid authentication credentials |
| `403 Forbidden` | The caller is not allowed to perform the operation | Authenticated caller lacks permission |
| `404 Not Found` | Resource does not exist or is not available | Requested resource is missing |
| `409 Conflict` | Request conflicts with current state | Duplicate creation or conflicting updates |
| `422 Unprocessable Content` | Content cannot be processed according to the API's rules | Semantic validation failure when the API uses this status |
| `500 Internal Server Error` | Unexpected server failure | Unhandled exception or unforeseen server-side failure |
| `503 Service Unavailable` | Service cannot currently handle the request | Temporary overload or unavailable dependency |

Choose a status code that accurately represents the outcome. Do not return `200 OK` for an operation that failed simply because the endpoint completed execution.

The exact response contract depends on the API. Consistency is important: clients should not have to guess whether a missing resource produces `404`, `200` with `null`, or some unrelated status.

### 11. ProblemDetails

`ProblemDetails` is a standard format for communicating API errors. ASP.NET Core supports it through `Microsoft.AspNetCore.Mvc.ProblemDetails` and related services.

A problem response can include:

- `status`: the HTTP status code.
- `title`: a short description of the problem.
- `detail`: additional client-safe information about this occurrence.
- `type`: a URI identifying the type of problem.
- `instance`: a URI identifying the specific occurrence, if appropriate.

For example:

```csharp
return Results.Problem(
    statusCode: StatusCodes.Status404NotFound,
    title: "Boid not found.",
    detail: "No boid exists with the requested identifier.");
```

For an API that uses consistent problem responses, register problem-details services:

```csharp
builder.Services.AddProblemDetails();
```

ASP.NET Core can use these services to generate problem-details responses for supported error-handling scenarios. Exact behavior depends on the middleware and endpoint configuration, so do not assume every non-success response will automatically become a problem response.

The response status code should match the `status` value in the body. A problem response should also avoid exposing internal exception messages or stack traces.

The `detail` field is not a place to dump diagnostic information. It should contain only information safe and useful for the client.

### 12. Validation errors

Validation errors are a common source of unsuccessful HTTP requests. The API should tell the client what is wrong without exposing internal implementation details.

Consider an endpoint that accepts a speed value:

```csharp
app.MapPost("/boids", (CreateBoidRequest request) =>
{
    if (request.Speed < 0)
    {
        return Results.ValidationProblem(
            new Dictionary<string, string[]>
            {
                ["speed"] = new[]
                {
                    "Speed must be non-negative."
                }
            });
    }

    return Results.Created(
        $"/boids/{request.Id}",
        request);
});

public sealed record CreateBoidRequest(
    int Id,
    double Speed);
```

The request is represented by a data-transfer object, or DTO. The endpoint checks the supplied value and returns a validation-problem response if it violates the rule.

This is an illustrative validation pattern, not a complete production API. A real endpoint would also validate the identifier, define its resource-creation semantics, and ensure that the resource is actually stored before returning `201 Created`.

Validation should distinguish between syntactically invalid requests and requests that conflict with application rules. Both require clear contracts so that clients can respond appropriately.

### 13. Logging HTTP requests

Request logging can help answer operational questions:

- Which endpoint was requested?
- What status code was returned?
- How long did the request take?
- Did the request fail?
- Which trace or correlation identifier belongs to the request?

ASP.NET Core's HTTP logging middleware can record selected request and response information. Depending on the configuration, it can capture metadata such as method, path, status code, and duration.

Request and response bodies should not be logged indiscriminately. They may contain passwords, tokens, personal information, or large payloads. Configure which fields are captured and apply redaction or filtering where necessary.

For application-specific events, structured `ILogger<T>` calls often provide more useful context than dumping entire HTTP messages.

For example:

```csharp
app.MapPost(
    "/boids/{id:int}/move",
    (int id, MoveRequest request, ILogger<Program> logger) =>
    {
        logger.LogInformation(
            "Movement requested for boid {BoidId}",
            id);

        return Results.Ok(new
        {
            id,
            request.DeltaX,
            request.DeltaY
        });
    });

public sealed record MoveRequest(
    double DeltaX,
    double DeltaY);
```

This records the requested operation without logging the entire request body. In a real application, the handler would perform the movement and validate the offsets before returning success.

### 14. Trace identifiers and correlation

When an application handles many concurrent requests, a log entry by itself may not reveal which request produced it.

ASP.NET Core supports request tracing through `Activity` and related diagnostic mechanisms. The current activity can provide a trace identifier that helps correlate related operations.

For example:

```csharp
app.MapGet("/diagnostics", (HttpContext context) =>
{
    return Results.Ok(new
    {
        traceId = context.TraceIdentifier
    });
});
```

`HttpContext.TraceIdentifier` provides an identifier for the current HTTP request. Distributed tracing can provide additional trace and span identifiers across multiple services.

Trace identifiers are useful for diagnostics, but they are not authentication credentials and should not be treated as proof of identity or authorization.

If a client is given a trace identifier in an error response, it can include that identifier in a support request. The application can then use it to find related logs, provided the identifier is recorded consistently.

Do not accept arbitrary client-supplied identifiers as trusted internal identity. If the application supports a correlation ID from a request header, validate and handle it deliberately.

### 15. Middleware ordering and error boundaries

Middleware ordering affects which failures can be handled and what information is available when a response is generated.

Exception handling middleware should normally execute before middleware and endpoints whose exceptions it is expected to catch.

For example:

```csharp
app.UseExceptionHandler();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/boids", () => Results.Ok());
```

This is a conceptual ordering example. Authentication and authorization middleware should be included and ordered according to the application's actual requirements. The minimal hosting model automatically wires some routing behavior, so explicit `UseRouting` is not required in every application.

Exception handling does not undo a response that has already been sent. If an endpoint has started writing a response body and then throws an exception, the middleware may be unable to replace the response with a clean `500` problem response.

Middleware that needs to observe response status codes or timing may also need to execute its logic around the downstream pipeline. Understanding this execution order is essential when debugging errors that appear to bypass logging or centralized handling.

### 16. Testing logging and error responses

Logging and error handling should be verified through tests rather than assumed to work because the application builds.

For a minimal API, an integration test can issue an HTTP request and assert the returned status code and response body. `WebApplicationFactory<TEntryPoint>` from `Microsoft.AspNetCore.Mvc.Testing` is commonly used to host an application in a test environment.

Useful tests include:

- A valid request returns the expected success status.
- An invalid request returns the expected client-error status.
- A nonexistent resource returns `404 Not Found`.
- An unexpected exception produces a generic server-error response.
- The response does not expose an internal exception message.
- A meaningful application event is logged at the intended level.

Unit tests can verify application logic in isolation. Integration tests verify the behavior of the HTTP pipeline, routing, middleware, and response generation together.

A test of an exception-handling endpoint should check both the status code and the client-visible response. Merely asserting that the request returned some response does not establish that the error was handled correctly.

## Challenge

Build a small ASP.NET Core Minimal API that demonstrates structured logging, centralized exception handling, and consistent HTTP error responses.

Your application must satisfy all of the following requirements:

1. **Create a Minimal API:** Define a `GET /boids/{id:int}` endpoint that returns a sample boid when the identifier exists.
2. **Handle missing resources:** Return `404 Not Found` when the requested boid does not exist.
3. **Validate input:** Return `400 Bad Request` for an invalid identifier or invalid request value where applicable.
4. **Use structured logging:** Inject `ILogger<T>` into at least one endpoint or service and log a meaningful application event with named properties.
5. **Configure exception handling:** Use centralized exception handling so unexpected exceptions do not expose internal details to clients.
6. **Use ProblemDetails:** Return consistent problem responses for at least one client error and the unexpected server-error path.
7. **Demonstrate an unexpected failure:** Add a temporary development endpoint that deliberately throws an exception. Verify that the client receives a generic `500 Internal Server Error` response.
8. **Keep diagnostics internal:** Log useful exception information for operators, but do not include the raw exception message or stack trace in the production response.
9. **Include trace context:** Include a request or trace identifier in diagnostic output or in a client-safe error response so a failure can be investigated.
10. **Test the API:** Use integration tests or an equivalent repeatable HTTP test to verify success, not-found, validation, and unexpected-exception behavior.
11. **Keep responsibilities clear:** Keep endpoint logic focused and use a service if doing so meaningfully separates application behavior from HTTP concerns.

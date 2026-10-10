# API Documentation & Testing

## Micro Goal

Learn how to document ASP.NET Core HTTP APIs with OpenAPI, explore endpoints through generated API documentation, and write automated tests that verify HTTP behavior, response status codes, payloads, and API contracts.

## Concepts Covered

- Why API documentation matters
- OpenAPI specifications
- OpenAPI document generation in ASP.NET Core
- Interactive API exploration
- Endpoint summaries and descriptions
- Request and response schemas
- HTTP status codes and API contracts
- Integration testing versus unit testing
- `WebApplicationFactory<TEntryPoint>`
- `Microsoft.AspNetCore.Mvc.Testing`
- `HttpClient` for HTTP integration tests
- Testing response status codes and JSON payloads
- Testing validation and error responses
- Testing route parameters and query strings
- Test isolation and repeatability
- Running API tests with `dotnet test`

## Completion Criteria

- [ ] Explain the difference between API documentation, unit testing, and integration testing.
- [ ] Explain the purpose of an OpenAPI document.
- [ ] Enable OpenAPI document generation in an ASP.NET Core application.
- [ ] Provide meaningful endpoint summaries and descriptions.
- [ ] Inspect documented routes, parameters, request bodies, responses, and status codes.
- [ ] Create a separate integration-test project for an ASP.NET Core application.
- [ ] Use `WebApplicationFactory<TEntryPoint>` to host the application for testing.
- [ ] Send HTTP requests through `HttpClient` in automated tests.
- [ ] Verify status codes, response bodies, and JSON values.
- [ ] Test both successful and unsuccessful API requests.
- [ ] Run the test suite with `dotnet test`.
- [ ] Explain what the tests verify and what remains outside their coverage.

## Lecture

### 1. Why API documentation and testing matter

An HTTP API defines a contract between an application and its clients. The contract describes which endpoints are available, which inputs they accept, what they return, and how they communicate errors.

For example, a Boids API might expose:

- `GET /api/boids` to retrieve boids.
- `GET /api/boids/1` to retrieve a specific boid.
- `POST /api/boids` to create a boid.
- `DELETE /api/boids/1` to remove a boid.

A client needs to know which requests are valid and what each response means. Documentation communicates that information to developers and tools. Automated tests verify that the application actually follows the documented contract.

Documentation and tests serve different purposes:

- **API documentation** describes the intended interface.
- **Unit tests** verify isolated application logic.
- **Integration tests** verify that multiple components work together, such as routing, request handling, serialization, and HTTP responses.

A documented endpoint can still be implemented incorrectly. A passing unit test does not necessarily prove that routing or HTTP serialization works correctly. Using documentation and integration tests together provides stronger evidence that an API behaves as intended.

### 2. OpenAPI

OpenAPI is a specification for describing HTTP APIs in a machine-readable format. An OpenAPI document describes available paths, HTTP methods, parameters, request bodies, response codes, and data schemas.

An OpenAPI document is commonly represented as JSON or YAML. Tools can use it to display interactive documentation, generate client code, or inspect an API's structure.

For example, an OpenAPI document might describe a response from `GET /api/boids` as an array of boid objects:

```json
{
  "paths": {
    "/api/boids": {
      "get": {
        "summary": "Get all boids",
        "responses": {
          "200": {
            "description": "Returns the available boids"
          }
        }
      }
    }
  }
}
```

This is an abbreviated illustration, not a complete OpenAPI document. A generated document normally contains additional metadata, schemas, and other details.

OpenAPI describes the API interface; it does not implement the endpoint or guarantee that the endpoint behaves correctly.

### 3. Generating OpenAPI documentation in ASP.NET Core

ASP.NET Core supports OpenAPI document generation. In current .NET versions, the built-in OpenAPI integration can be registered with services and mapped as an endpoint.

For a Minimal API application, the relevant configuration commonly looks like this:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/boids", () =>
{
    return Results.Ok(new[]
    {
        new { Id = 1, X = 10.0, Y = 20.0 },
        new { Id = 2, X = 30.0, Y = 40.0 }
    });
});

app.Run();
```

The important calls are:

- `AddOpenApi()` registers the services that generate the OpenAPI document.
- `MapOpenApi()` exposes an HTTP endpoint that serves the generated document.

With the default route configuration, the document is commonly available at `/openapi/v1.json`. The exact route can be customized, so verify the application's configuration rather than assuming every project uses the default.

The example maps the document only in the Development environment. This is a common choice, but whether documentation should be publicly exposed in production is a deployment decision.

The built-in OpenAPI integration generates a specification. It does not automatically provide a graphical interface for browsing it. An interactive UI can be added through a compatible third-party package or another documentation tool.

### 4. Exploring an API

When an API is running locally, you can inspect its HTTP endpoints using a browser, command-line HTTP client, or interactive API UI.

For example, with the application running on the indicated local address:

```bash
curl -i http://localhost:5000/api/boids
```

The port is only an example. ASP.NET Core's actual local address depends on the project's launch configuration and hosting settings.

The `-i` option asks `curl` to display the HTTP response headers along with the response body. This helps you inspect the status code and content type as well as the returned data.

For the example endpoint, the response body is JSON representing an array of boids. The status code should be `200 OK` because the handler returns `Results.Ok(...)`.

API exploration helps you compare actual behavior with documentation. It is useful during development, but manually inspecting responses is not a substitute for repeatable automated tests.

### 5. Documenting endpoint behavior

Useful API documentation should explain more than a route's name. It should clarify what the endpoint does, what inputs it accepts, and which outcomes a client should expect.

For example, consider an endpoint that retrieves one boid by identifier:

```csharp
app.MapGet("/api/boids/{id:int}", (int id) =>
{
    if (id <= 0)
    {
        return Results.BadRequest();
    }

    return Results.Ok(new
    {
        Id = id,
        X = 10.0,
        Y = 20.0
    });
})
.WithSummary("Get a boid by ID")
.WithDescription("Returns the boid associated with a positive integer ID.");
```

The route constraint `:int` restricts the route parameter to an integer. The handler also rejects non-positive integer identifiers.

The summary and description add information to the endpoint's metadata, which OpenAPI generators can use when producing documentation.

For a production-quality API, also document meaningful response outcomes, including relevant success and error status codes. Metadata support and the exact generated schema can vary depending on the endpoint style and the OpenAPI tooling being used.

### 6. HTTP status codes are part of the contract

Clients use status codes to determine the outcome of a request. Tests should verify that the API returns the intended status code, not merely that a response body exists.

Common status codes include:

- `200 OK`: the request succeeded and returned a response.
- `201 Created`: a resource was created successfully.
- `204 No Content`: the request succeeded and no response body is returned.
- `400 Bad Request`: the request was invalid.
- `401 Unauthorized`: valid authentication credentials are missing or invalid.
- `403 Forbidden`: the caller is not permitted to perform the operation.
- `404 Not Found`: the requested resource or endpoint was not found.
- `409 Conflict`: the request conflicts with the current state of a resource.
- `500 Internal Server Error`: an unexpected server error occurred.

The correct status depends on the endpoint's contract. For example, a creation endpoint may return `201 Created`, while a deletion endpoint may return `204 No Content`.

A useful integration test checks both the status code and the response payload when a payload is part of the contract. This prevents a test from passing when the endpoint returns the correct status but incorrect data.

### 7. Integration testing ASP.NET Core

A unit test can verify a calculation without starting an HTTP server. An integration test can exercise the ASP.NET Core request pipeline and verify the behavior of the application as a web API.

ASP.NET Core provides `WebApplicationFactory<TEntryPoint>` through the `Microsoft.AspNetCore.Mvc.Testing` package. It can create an in-memory test host for an application, allowing tests to send HTTP requests through `HttpClient` without manually starting a separate server.

A typical structure is:

```text
BoidsApi/
├── BoidsApi.csproj
└── Program.cs

BoidsApi.Tests/
├── BoidsApi.Tests.csproj
└── BoidsApiTests.cs
```

The test project references the web application project and uses a compatible test framework and test host package.

For a Minimal API project, the application entry point may need to be exposed to the test assembly. A common approach is to add the following at the end of `Program.cs`:

```csharp
public partial class Program { }
```

This makes the generated `Program` type accessible to the test project when used as the `WebApplicationFactory<Program>` entry point.

A test project using xUnit commonly uses the `Microsoft.AspNetCore.Mvc.Testing` package and the appropriate test SDK and xUnit packages. Use package versions compatible with the target framework and the existing project.

### 8. Writing an HTTP integration test

Suppose the web application exposes `GET /api/boids` and returns a JSON array of boid objects.

An integration test can request that endpoint and check the result:

```csharp
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BoidsApi.Tests;

public class BoidsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BoidsApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetBoids_ReturnsOkAndBoidData()
    {
        // Act
        HttpResponseMessage response =
            await _client.GetAsync("/api/boids");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        BoidResponse[]? boids =
            await response.Content.ReadFromJsonAsync<BoidResponse[]>();

        Assert.NotNull(boids);
        Assert.NotEmpty(boids);
        Assert.Equal(1, boids[0].Id);
    }

    private sealed record BoidResponse(int Id, double X, double Y);
}
```

This example assumes that the API returns an array with `Id`, `X`, and `Y` properties and that the first element has an identifier of `1`. Adjust the response model and expected values to match the actual API contract.

`IClassFixture<WebApplicationFactory<Program>>` lets xUnit share the test factory for the test class. `CreateClient()` creates an HTTP client that sends requests to the test host.

The test checks the status code, deserializes the JSON response, and verifies that data was returned. It exercises routing, request handling, and serialization rather than calling the endpoint delegate directly.

### 9. Testing error responses

Successful requests are only part of an API's behavior. Tests should also verify invalid inputs and missing resources.

Suppose the API exposes `GET /api/boids/{id:int}` and returns `404 Not Found` when a positive identifier does not exist. An integration test could verify that contract:

```csharp
[Fact]
public async Task GetUnknownBoid_ReturnsNotFound()
{
    // Act
    HttpResponseMessage response =
        await _client.GetAsync("/api/boids/999999");

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}
```

This example assumes that the application has no boid with that identifier and that the endpoint is designed to return `404` for missing resources. If the API uses a different contract, the test should reflect that contract.

Error tests are particularly useful for verifying:

- Invalid route parameters.
- Invalid request bodies.
- Missing resources.
- Validation failures.
- Unauthorized or forbidden requests when authentication is configured.
- Consistent error response formats.

Avoid testing only that an error occurred. Check the appropriate status code and, when applicable, the documented error payload.

### 10. Testing request bodies and JSON responses

Many APIs accept JSON request bodies. For example, a creation endpoint might accept an object containing an initial position and movement speed.

`HttpClient` can send JSON using `PostAsJsonAsync`:

```csharp
var request = new
{
    X = 10.0,
    Y = 20.0,
    Speed = 2.0
};

HttpResponseMessage response =
    await _client.PostAsJsonAsync("/api/boids", request);
```

A test should verify the expected status code and inspect the returned resource when the endpoint promises to return one.

If the API returns `201 Created`, it may also include a `Location` header pointing to the created resource. The integration test can verify that behavior if it is part of the contract.

For invalid JSON data or invalid field values, test the API's documented validation behavior. Depending on how the endpoint is implemented, invalid requests might return `400 Bad Request` or another appropriate response.

### 11. Testing validation and endpoint metadata

Validation rules are part of the API's contract. If an endpoint requires a positive speed, a missing identifier, negative speed, or malformed request should produce a predictable response.

Tests should cover both valid and invalid inputs. For example, if negative speed is rejected, verify that the API returns the documented status code and does not create a resource.

OpenAPI documentation can also be inspected to verify that endpoints are described and schemas are generated as expected. However, checking the generated document is separate from testing the actual HTTP behavior. Both may be useful:

- An OpenAPI test checks the published description.
- An integration test checks the real request and response behavior.

Do not assume that documenting a validation rule automatically enforces it. The endpoint must implement or invoke the validation logic.

### 12. Test isolation and repeatability

Integration tests should produce predictable results without depending on execution order or external state.

If an endpoint writes to a database, tests may need a dedicated test database or a replaceable test data store. If an endpoint uses external services, consider configuring test substitutes so that tests do not depend on network availability.

`WebApplicationFactory<TEntryPoint>` supports customization of the test application's service configuration. This allows a test to replace certain registrations or configure test-specific settings.

Do not use production credentials or production data in automated tests. Keep the test environment isolated, and ensure that each test arranges the state it needs.

The in-memory test server exercises the ASP.NET Core pipeline, but it does not automatically test every production hosting concern, such as reverse proxies, TLS termination, deployed infrastructure, or external network behavior. Those may require separate deployment or end-to-end tests.

### 13. Running and diagnosing API tests

Run the test project from the repository root or from the directory containing its project file:

```bash
dotnet test path/to/BoidsApi.Tests.csproj
```

If tests fail, inspect:

1. The test name and failure message.
2. The actual status code versus the expected status code.
3. The returned JSON and its structure.
4. The endpoint's route template and handler logic.
5. Whether the test setup matches the expected application configuration.
6. Whether the API contract or the implementation needs to change.

A failed test may indicate a bug in the endpoint, a wrong expectation, or an incomplete test setup. Do not simply change an expected status code to make the test pass; first determine what the API is supposed to do.

After correcting the issue, rerun the failing test and then the full suite.

### 14. Documentation and tests as complementary tools

OpenAPI documentation and automated tests reinforce each other but do not replace one another.

OpenAPI helps consumers understand what the API claims to support. Integration tests check whether the application behaves as expected when real HTTP requests are sent through its request pipeline.

For a maintainable API, keep the documentation aligned with implementation and tests. When an endpoint's contract changes, update the endpoint behavior, its documentation, and the relevant tests together.

## Challenge

Build a small ASP.NET Core Minimal API for managing boids, then document and test its HTTP behavior.

Your solution must satisfy all of the following requirements:

1. **Create a Minimal API:** Expose `GET /api/boids` to return a list of boids.
2. **Retrieve one resource:** Expose `GET /api/boids/{id:int}` to return a boid by identifier or `404 Not Found` when it does not exist.
3. **Create a resource:** Expose `POST /api/boids` to accept a JSON request containing coordinates and speed. Return the appropriate success status and created resource.
4. **Validate input:** Reject negative speed and other invalid request data with a documented client-error response. Do not create a resource when validation fails.
5. **Generate OpenAPI documentation:** Register OpenAPI services and map the OpenAPI endpoint. Keep its exposure appropriate for the configured environment.
6. **Document the endpoints:** Add meaningful summaries and descriptions. Ensure the documented routes, request data, and responses reflect the implemented behavior.
7. **Create a separate test project:** Use xUnit and `Microsoft.AspNetCore.Mvc.Testing`, reference the web application, and expose the entry point if needed for `WebApplicationFactory<Program>`.
8. **Test successful retrieval:** Verify that `GET /api/boids` returns the expected status and JSON data.
9. **Test missing resources:** Verify that requesting an unknown boid returns `404 Not Found`.
10. **Test creation:** Send a valid JSON request to `POST /api/boids` and verify the expected status and returned resource.
11. **Test invalid input:** Submit a negative speed and verify that the API returns the documented client-error response without creating a boid.
12. **Test documentation:** Request the generated OpenAPI document and verify that it includes the expected API paths.
13. **Run the tests:** Use `dotnet test` and confirm that the complete test suite passes.
14. **Keep the implementation focused:** Use simple, deterministic logic and a clear API contract. Do not introduce authentication, databases, or unrelated infrastructure for this exercise.

The goal is to demonstrate that you can document an ASP.NET Core API and verify its actual HTTP behavior using automated integration tests.
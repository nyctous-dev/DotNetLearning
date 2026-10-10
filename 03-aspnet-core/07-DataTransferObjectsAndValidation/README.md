# Data Transfer Objects & Validation

## Micro Goal

Learn how to define request and response DTOs for ASP.NET Core APIs, validate incoming data, return meaningful HTTP error responses, and keep external API contracts separate from internal application models.

## Concepts Covered

- Data Transfer Objects (DTOs)
- Request DTOs and response DTOs
- Separating API contracts from domain models
- JSON request-body binding
- ASP.NET Core model binding
- Data annotations and validation attributes
- Automatic validation behavior in Minimal APIs
- Manual validation and validation results
- Required values, string lengths, numeric ranges, and formats
- Nullable properties and optional request fields
- HTTP 400 Bad Request responses
- Validation problem details
- `IResult`, `Results`, and `TypedResults`
- Input validation versus business-rule validation
- Avoiding overposting and exposing internal properties
- Testing valid and invalid API requests

## Completion Criteria

- [ ] Explain why APIs use DTOs instead of exposing internal models directly.
- [ ] Define separate request and response DTOs.
- [ ] Bind a JSON request body to a DTO in an ASP.NET Core Minimal API.
- [ ] Apply validation attributes to request properties.
- [ ] Understand why binding a DTO does not automatically guarantee that its data is valid.
- [ ] Validate incoming requests and return an appropriate HTTP 400 response.
- [ ] Return structured validation errors that identify invalid fields.
- [ ] Distinguish input validation from business-rule validation.
- [ ] Prevent clients from setting properties that should be controlled by the server.
- [ ] Verify validation behavior using HTTP requests.
- [ ] Complete the challenge with a clean separation between endpoint logic, DTOs, and application models.

## Lecture

### 1. What is a Data Transfer Object?

A Data Transfer Object (DTO) is an object designed to carry data across a boundary, such as an HTTP API request or response. A DTO defines the shape of the data that an API accepts or returns.

Consider a Boids application that allows a client to create a simulation. The client might send a simulation name and an initial boid count. The server does not necessarily need to accept every property used internally by the simulation.

A request DTO could look like this:

```csharp
public sealed class CreateSimulationRequest
{
    public string Name { get; init; } = string.Empty;

    public int InitialBoidCount { get; init; }
}
```

This type describes the data expected when a client asks to create a simulation.

A response DTO could expose the resulting simulation identifier and its initial state:

```csharp
public sealed class SimulationResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public int BoidCount { get; init; }
}
```

The request and response DTOs have different purposes. The request describes what a client may submit. The response describes what the API chooses to return.

A DTO is not inherently a database model, domain model, or business object. It is an API-facing data contract. Keeping these responsibilities separate makes it easier to evolve the API without exposing implementation details.

### 2. Why not expose internal models directly?

Suppose an internal simulation model contains more information than the client should control or see:

```csharp
public sealed class Simulation
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int BoidCount { get; set; }

    public bool IsRunning { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
```

If an API accepts this model directly as its request body, clients may be able to submit properties such as `Id`, `IsRunning`, or `CreatedAtUtc`, even though those values should be controlled by the server.

This creates an overposting risk: clients can attempt to assign fields that should not be client-controlled.

A dedicated request DTO reduces this risk by accepting only the properties that are part of the intended request contract.

```csharp
public sealed class CreateSimulationRequest
{
    public string Name { get; init; } = string.Empty;

    public int InitialBoidCount { get; init; }
}
```

The server can create the internal model using trusted server-side values:

```csharp
Simulation simulation = new()
{
    Id = Guid.NewGuid(),
    Name = request.Name,
    BoidCount = request.InitialBoidCount,
    IsRunning = false,
    CreatedAtUtc = DateTime.UtcNow
};
```

The client does not supply the identifier, running state, or creation timestamp through this DTO.

DTOs are not a complete authorization or security system, but they help define which values a client is allowed to submit and which values the server controls.

### 3. Request DTOs and response DTOs

Request and response DTOs should be designed around the operation they support.

For example, creating a simulation and returning a simulation are different operations. A creation request might contain a name and initial count, while a response might contain the generated identifier, name, count, and running state.

```csharp
public sealed class CreateSimulationRequest
{
    public string Name { get; init; } = string.Empty;

    public int InitialBoidCount { get; init; }
}

public sealed class SimulationResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public int BoidCount { get; init; }

    public bool IsRunning { get; init; }
}
```

A response can be constructed explicitly from the internal model:

```csharp
SimulationResponse response = new()
{
    Id = simulation.Id,
    Name = simulation.Name,
    BoidCount = simulation.BoidCount,
    IsRunning = simulation.IsRunning
};
```

This explicit mapping makes the API contract visible. If the internal model later gains a property, it will not automatically become part of the response unless you choose to expose it.

For small applications, manual mapping is often sufficient. Mapping libraries may help when an application has many models and DTOs, but they are not necessary for this module.

### 4. JSON request-body binding in Minimal APIs

ASP.NET Core Minimal APIs can bind a request body to a complex parameter. For JSON requests, the body is deserialized into the target .NET type.

For example:

```csharp
app.MapPost(
    "/simulations",
    (CreateSimulationRequest request) =>
    {
        return Results.Ok(request);
    });
```

A client can send a request body such as:

```json
{
  "name": "Boids Demo",
  "initialBoidCount": 100
}
```

The endpoint receives a `CreateSimulationRequest` instance when the request body can be read and deserialized successfully.

JSON property names are commonly written in camel case, while C# properties use Pascal case. ASP.NET Core's default web JSON settings support this convention.

The client must send an appropriate content type, normally `application/json`, when submitting a JSON body.

**Important distinction:** successful deserialization does not necessarily mean the data is valid. A request can deserialize into a DTO while containing an empty name or a negative count. Binding and validation are separate concerns.

If the request body contains malformed JSON or a value that cannot be converted to the expected type, ASP.NET Core may reject the request before the endpoint receives a usable DTO. This differs from a well-formed request whose values violate application rules.

### 5. Validation with data annotations

Data annotations let you declare common validation rules on DTO properties.

Common attributes include:

- `[Required]` — requires a value under the attribute's validation rules.
- `[StringLength]` — restricts a string's length.
- `[MinLength]` and `[MaxLength]` — constrain minimum and maximum lengths.
- `[Range]` — restricts a numeric value to an allowed range.
- `[RegularExpression]` — requires a string to match a pattern.

A request DTO might use several of these attributes:

```csharp
using System.ComponentModel.DataAnnotations;

public sealed class CreateSimulationRequest
{
    [Required]
    [StringLength(80, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Range(1, 500)]
    public int InitialBoidCount { get; init; }
}
```

These declarations express that the name must satisfy the required and length rules, and the initial count must be between 1 and 500.

A validation attribute declares a rule. Whether and when that rule is executed depends on the application code or framework behavior.

This is an important ASP.NET Core distinction: **Minimal APIs do not automatically execute all data annotation validation merely because a DTO has validation attributes.** You must explicitly invoke validation or use a supported validation mechanism.

The `[ApiController]` behavior often encountered in controller-based APIs includes automatic model-state validation responses. That behavior should not be assumed for a Minimal API endpoint.

### 6. Validating a DTO in a Minimal API

For a small Minimal API, you can invoke `Validator.TryValidateObject` to execute the data annotation rules on a DTO.

```csharp
using System.ComponentModel.DataAnnotations;

static Dictionary<string, string[]> ValidateRequest<T>(T request)
{
    var validationResults = new List<ValidationResult>();

    var validationContext = new ValidationContext(request!);

    bool isValid = Validator.TryValidateObject(
        request!,
        validationContext,
        validationResults,
        validateAllProperties: true);

    if (isValid)
    {
        return new Dictionary<string, string[]>();
    }

    return validationResults
        .SelectMany(
            result => result.MemberNames.DefaultIfEmpty(string.Empty),
            (result, memberName) => new
            {
                MemberName = memberName,
                ErrorMessage = result.ErrorMessage ?? "Invalid value."
            })
        .GroupBy(
            item => item.MemberName,
            StringComparer.OrdinalIgnoreCase)
        .ToDictionary(
            group => group.Key,
            group => group.Select(item => item.ErrorMessage).ToArray(),
            StringComparer.OrdinalIgnoreCase);
}
```

This helper returns an empty dictionary when validation succeeds. Otherwise, it groups validation messages by the associated property name.

The helper uses LINQ, so the project needs `System.Linq`. Modern .NET templates often enable implicit global usings, but explicit `using` directives can be added when necessary.

You can use it inside an endpoint:

```csharp
app.MapPost(
    "/simulations",
    (CreateSimulationRequest request) =>
    {
        Dictionary<string, string[]> errors =
            ValidateRequest(request);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        return Results.Ok(request);
    });
```

`Results.ValidationProblem` returns a structured validation error response with HTTP status code 400. The response identifies the validation errors by field, allowing clients to display useful messages.

The helper is suitable for demonstrating basic data annotation validation. It does not replace all possible validation requirements: `TryValidateObject` does not recursively validate every nested object automatically, and custom cross-property rules may need additional logic.

### 7. Structured validation errors

A client should be able to identify which fields failed validation. Returning only a plain message such as `"Invalid request"` often provides too little information.

A validation problem response follows the Problem Details conventions and includes a dictionary of errors. For example, a response might contain:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": [
      "The Name field is required."
    ],
    "InitialBoidCount": [
      "The field InitialBoidCount must be between 1 and 500."
    ]
  }
}
```

The exact messages and response details depend on the validation attributes and framework configuration.

HTTP 400 Bad Request is appropriate when the request cannot be accepted because the client submitted invalid input.

Other status codes have different meanings. For example, 404 Not Found means the requested resource or route could not be found, while 500 Internal Server Error indicates an unexpected server-side failure.

Do not return HTTP 200 for a request that failed validation. The status code should accurately represent the outcome.

### 8. Binding failures versus validation failures

Request processing has several distinct stages.

1. ASP.NET Core matches the request to an endpoint.
2. The framework binds route values, query parameters, headers, and body data to endpoint parameters.
3. JSON request bodies are deserialized into their target types.
4. Application validation checks whether the resulting values satisfy the required rules.
5. The endpoint performs its operation and returns a response.

A failure at one stage is not necessarily the same as a failure at another.

For example:

- A negative boid count may deserialize successfully into an integer, but fail the `[Range]` rule.
- A string where a JSON integer is expected may fail during deserialization.
- A valid DTO may still violate a business rule, such as trying to start a simulation that has already been stopped or deleted.

Understanding these differences helps you choose the correct place to handle each error.

### 9. Input validation versus business-rule validation

Input validation checks whether submitted data has an acceptable shape and value range.

Examples include:

- A required name is missing.
- A string exceeds its maximum length.
- A boid count is outside an allowed numeric range.
- A field contains a malformed identifier.

Business-rule validation checks whether an operation is allowed within the application's domain.

Examples include:

- A simulation cannot start because it has no boids.
- A requested simulation does not exist.
- A user is not authorized to modify a simulation.
- A simulation cannot be changed while an incompatible operation is running.

Data annotations are useful for straightforward input rules. Business rules usually belong in application services or domain logic, where they can be applied consistently regardless of whether the operation was triggered by an HTTP request, a background task, or another part of the application.

Do not place all business logic in DTO validation attributes. A DTO describes incoming data; it should not become responsible for managing the entire application's state.

### 10. Nullable and optional fields

Some request fields are optional. For example, an update operation might allow a client to change a name without changing the boid count.

A nullable property can represent a value that may be absent:

```csharp
public sealed class UpdateSimulationRequest
{
    [StringLength(80, MinimumLength = 1)]
    public string? Name { get; init; }

    [Range(1, 500)]
    public int? BoidCount { get; init; }
}
```

Here, `Name` and `BoidCount` may be null. When a client sends a non-null value, the relevant validation rules apply.

Nullable reference annotations such as `string?` help the C# compiler analyze null usage. They do not independently enforce request validation at runtime.

Also, a null property does not always distinguish between an omitted JSON property and a property explicitly set to `null`. If an API needs to distinguish those cases—for example, in a partial-update contract—it requires a more deliberate request model or binding strategy.

Choose optional fields based on the operation's intended behavior, not merely because nullable types are available.

### 11. Returning appropriate endpoint results

Minimal APIs can return results through `Results` or `TypedResults`.

For example:

```csharp
app.MapGet(
    "/simulations/{id:guid}",
    (Guid id) =>
    {
        SimulationResponse? simulation = FindSimulation(id);

        if (simulation is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(simulation);
    });
```

This example illustrates the response choices; `FindSimulation` would need to be implemented by the application.

Common results include:

- `Results.Ok(value)` for HTTP 200.
- `Results.Created(uri, value)` for HTTP 201 when a resource is created.
- `Results.BadRequest()` for HTTP 400.
- `Results.ValidationProblem(errors)` for HTTP 400 with structured validation errors.
- `Results.NotFound()` for HTTP 404.
- `Results.NoContent()` for HTTP 204 when no response body is needed.

Choose the result that matches the operation. For example, a successful resource creation commonly returns 201 Created and may include a location for the new resource.

`Results` provides a convenient way to return HTTP results from Minimal API handlers. `TypedResults` offers strongly typed result types that can improve type checking and endpoint metadata in some scenarios.

### 12. Testing validation behavior

Validation should be tested at the boundary where the API accepts data, not only by testing individual DTO properties in isolation.

Useful cases include:

- A valid request is accepted.
- A missing or empty name is rejected.
- A count below the minimum is rejected.
- A count above the maximum is rejected.
- A malformed JSON body receives an appropriate client error.
- An invalid request produces HTTP 400 and identifies the failing fields.
- A valid request produces the expected success response.

You can test endpoints by sending HTTP requests using tools such as `curl`, an API client, or automated integration tests.

For example, a valid JSON body might look like:

```json
{
  "name": "Boids Demo",
  "initialBoidCount": 100
}
```

An invalid body might use a negative count:

```json
{
  "name": "Boids Demo",
  "initialBoidCount": -10
}
```

The invalid request should receive a client error when the validation rules are correctly wired into the endpoint.

Testing the HTTP response verifies more than testing the validation helper alone. It confirms that the endpoint actually invokes validation and translates failures into the expected HTTP result.

### 13. Keep the API contract deliberate

DTOs and validation work together to establish a clear API contract.

A request DTO defines which values clients may submit. Validation rules define which values are acceptable. The endpoint coordinates binding, validation, and response selection. Application or domain logic enforces rules that depend on the system's state.

Keeping those responsibilities distinct prevents an endpoint from becoming a large block of unrelated logic.

As the Boids application grows, the same principles will help separate HTTP-specific models from simulation logic. The simulation should not need to know how a JSON request was formatted, and the API should not expose every internal property simply because it exists.

## Challenge

Build a Minimal API endpoint that accepts requests to create a Boids simulation. Define separate request and response DTOs, validate incoming data, and return appropriate HTTP responses.

Your solution must satisfy all of the following requirements:

1. **Create a request DTO:** Define a `CreateSimulationRequest` with a name and initial boid count.
2. **Create a response DTO:** Define a `SimulationResponse` with a generated identifier, name, boid count, and running state.
3. **Add validation attributes:** Require a valid name of a reasonable length and restrict the initial boid count to a range such as 1–500.
4. **Define a Minimal API endpoint:** Map `POST /simulations` and bind the JSON request body to `CreateSimulationRequest`.
5. **Execute validation explicitly:** Do not assume that adding data annotation attributes automatically validates a Minimal API request. Implement and invoke a validation mechanism.
6. **Return structured validation errors:** When validation fails, return HTTP 400 with field-level error details.
7. **Create the internal model:** Only after validation succeeds, create the simulation with a server-generated identifier, an initial running state of `false`, and the submitted name and boid count.
8. **Return the response DTO:** Map the internal model to `SimulationResponse` and return HTTP 201 Created with the new resource's location.
9. **Keep responsibilities separate:** Keep request and response DTOs separate from the internal simulation model. Avoid putting the endpoint's entire implementation into a DTO.
10. **Test the endpoint:** Send one valid request and at least two invalid requests. Verify status codes and inspect the response bodies.
11. **Handle binding errors:** Test a malformed JSON body or a value with the wrong JSON type and observe how ASP.NET Core handles the failure.
12. **Explain the behavior:** Be able to distinguish a binding failure, a DTO validation failure, and a business-rule failure.

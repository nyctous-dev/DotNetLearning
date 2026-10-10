# HTTP & REST APIs

## Micro Goal

Understand how clients and web servers communicate over HTTP, how requests and responses are structured, and how REST conventions guide the design of predictable web APIs in ASP.NET Core.

## Concepts Covered

- Client-server communication
- HTTP requests and responses
- HTTP methods: `GET`, `POST`, `PUT`, `PATCH`, and `DELETE`
- HTTP request targets, headers, and bodies
- HTTP status codes and their meanings
- Path parameters and query parameters
- JSON request and response bodies
- Resource-oriented API design
- REST principles and statelessness
- Resource identifiers and URI design
- CRUD operations and their HTTP mappings
- Idempotency and safe methods
- Content types and `Content-Type` / `Accept` headers
- HTTP error responses
- ASP.NET Core endpoints as API handlers
- Testing HTTP endpoints with command-line tools

## Completion Criteria

- [ ] Explain the roles of the client, server, request, and response in HTTP communication.
- [ ] Identify the method, target, headers, and body of an HTTP request.
- [ ] Explain the purpose of common HTTP status codes.
- [ ] Distinguish path parameters from query parameters.
- [ ] Explain how JSON is used to represent API data.
- [ ] Map CRUD operations to appropriate HTTP methods.
- [ ] Explain the difference between safe and idempotent HTTP methods.
- [ ] Design resource-oriented routes that follow REST conventions.
- [ ] Return appropriate status codes and response bodies from ASP.NET Core endpoints.
- [ ] Test API endpoints using `curl` or another HTTP client.
- [ ] Explain why HTTP APIs should generally remain stateless between requests.

## Lecture

### 1. What HTTP is

HTTP stands for Hypertext Transfer Protocol. It is the application-layer protocol commonly used for communication between web clients and servers.

A client might be a browser, a mobile application, a command-line tool, or another server. The server receives a request, processes it, and sends back a response.

For example, a browser might request a list of boids from a web application. The client sends an HTTP request, the ASP.NET Core application processes it, and the server returns a response containing data.

The basic exchange looks like this:

1. The client sends a request to a server.
2. The server interprets the request and performs the required operation.
3. The server sends a response containing a status code and, when appropriate, headers and a body.
4. The client interprets the response.

HTTP is the communication protocol. ASP.NET Core is a framework that helps you implement the server-side behavior that responds to HTTP requests.

### 2. Structure of an HTTP request

An HTTP request communicates what the client wants the server to do and provides any necessary information.

A request can contain:

- **Method:** The operation the client wants to perform.
- **Target:** The resource path and possibly a query string.
- **Headers:** Metadata about the request.
- **Body:** Optional data sent to the server.

For example:

```http
GET /api/boids?limit=10 HTTP/1.1
Host: localhost:5000
Accept: application/json
```

This request asks for the boid resources at `/api/boids`, with a query parameter named `limit` set to `10`. The `Accept` header indicates that the client prefers a JSON response.

A request body is not necessary for every operation. A `GET` request typically retrieves information without sending a body. A `POST` request that creates a resource commonly includes a JSON body.

For example:

```http
POST /api/boids HTTP/1.1
Host: localhost:5000
Content-Type: application/json
Accept: application/json

{
  "x": 10.0,
  "y": 20.0,
  "speed": 2.5
}
```

The `Content-Type` header describes the format of the request body. Here, it tells the server that the submitted data is JSON.

Headers and bodies serve different purposes. Headers describe the request or response, while the body carries the data being transmitted.

### 3. Structure of an HTTP response

The server responds with a status code and may include headers and a body.

For example:

```http
HTTP/1.1 201 Created
Content-Type: application/json
Location: /api/boids/42

{
  "id": 42,
  "x": 10.0,
  "y": 20.0,
  "speed": 2.5
}
```

This response indicates that the server successfully created a resource. The `Location` header identifies the newly created resource, and the JSON body represents it.

A response generally contains:

- **Status code:** The outcome of the request.
- **Headers:** Metadata about the response.
- **Body:** Optional returned data or error information.

The response body is not always JSON. A server can return plain text, HTML, binary data, or no body at all, depending on the endpoint and status code.

### 4. HTTP methods

HTTP methods describe the intended operation on a resource. For APIs, the most common methods are `GET`, `POST`, `PUT`, `PATCH`, and `DELETE`.

| Method | Typical purpose | Example |
|---|---|---|
| `GET` | Retrieve a resource or collection | `GET /api/boids` |
| `POST` | Create a resource or submit an operation | `POST /api/boids` |
| `PUT` | Create or replace a resource at a known URI, depending on API design | `PUT /api/boids/42` |
| `PATCH` | Partially update a resource | `PATCH /api/boids/42` |
| `DELETE` | Delete a resource | `DELETE /api/boids/42` |

These methods have conventional meanings, but the endpoint's documented behavior ultimately determines what the API does.

A `GET` request should retrieve data rather than modify application state. A `POST` request often creates a resource, but it can also initiate an operation that does not fit ordinary CRUD.

`PUT` generally represents replacing a resource's state at the target URI. `PATCH` is commonly used when only selected fields need to change.

For example, a client might update a boid's position with `PATCH`:

```http
PATCH /api/boids/42 HTTP/1.1
Content-Type: application/json

{
  "x": 15.0,
  "y": 18.0
}
```

The API needs to define which fields may be updated and how omitted fields are handled. HTTP does not automatically determine those application-specific rules.

### 5. CRUD and resource-oriented APIs

CRUD stands for Create, Read, Update, and Delete. These are common data operations used when designing APIs around resources.

For a boid collection, an API might expose the following routes:

| Operation | Method | Route |
|---|---|---|
| List boids | `GET` | `/api/boids` |
| Get one boid | `GET` | `/api/boids/42` |
| Create a boid | `POST` | `/api/boids` |
| Replace a boid | `PUT` | `/api/boids/42` |
| Partially update a boid | `PATCH` | `/api/boids/42` |
| Delete a boid | `DELETE` | `/api/boids/42` |

The collection route `/api/boids` represents the collection of boid resources. The route `/api/boids/42` represents one specific resource.

Resource-oriented APIs generally use nouns in route paths and use HTTP methods to express the intended operation. For example, `DELETE /api/boids/42` is usually clearer than creating a custom route such as `/api/deleteBoid/42`.

Not every operation fits neatly into CRUD. An operation such as starting a simulation may require a carefully designed action endpoint or a resource representing the simulation's state. The important principle is to choose routes and methods that communicate the API's behavior consistently.

### 6. HTTP status codes

A status code tells the client the general outcome of the request. Status codes are grouped into five classes.

| Class | Meaning | Example |
|---|---|---|
| `1xx` | Informational | `100 Continue` |
| `2xx` | Success | `200 OK` |
| `3xx` | Redirection | `301 Moved Permanently` |
| `4xx` | Client-side request issue | `404 Not Found` |
| `5xx` | Server-side failure | `500 Internal Server Error` |

Important API status codes include:

- **`200 OK`** — The request succeeded. Often used when returning data.
- **`201 Created`** — A resource was successfully created.
- **`202 Accepted`** — The request was accepted for processing, but processing may not be complete.
- **`204 No Content`** — The request succeeded and there is no response body.
- **`400 Bad Request`** — The request is invalid or malformed.
- **`401 Unauthorized`** — Authentication is required or has failed. Despite the name, this status is about authentication.
- **`403 Forbidden`** — The server understands the request but refuses to authorize it.
- **`404 Not Found`** — The requested resource does not exist or is not available at that route.
- **`409 Conflict`** — The request conflicts with the current state of a resource.
- **`415 Unsupported Media Type`** — The server does not support the submitted request body's media type.
- **`422 Unprocessable Content`** — The content is understood but cannot be processed according to the application's rules.
- **`500 Internal Server Error`** — The server encountered an unexpected failure.

Choose status codes based on the actual outcome rather than returning `200 OK` for every situation. A client needs to distinguish a successful operation from a missing resource or invalid request.

The response body can provide additional information, but it should not contradict the status code. For example, returning `200 OK` with an error message in the body makes it harder for clients to interpret the result correctly.

### 7. Path parameters and query parameters

A path parameter identifies a resource or a component of a route. A query parameter usually modifies how a collection is filtered, sorted, searched, or paginated.

Consider these URLs:

```text
/api/boids/42
/api/boids?limit=10
/api/boids?minSpeed=2.0
/api/boids?sortBy=speed&descending=true
```

In `/api/boids/42`, `42` identifies a specific boid.

In `/api/boids?limit=10`, the query parameter `limit` asks for at most ten results, assuming the API supports that option.

A route may contain multiple query parameters, separated by `&`. Query parameter names and supported values are defined by the API.

Do not assume that a query parameter has an effect just because it appears in a URL. The server must implement the behavior, validate the supplied value, and define what happens when the parameter is missing or invalid.

### 8. JSON and API data

JSON stands for JavaScript Object Notation. It is a text-based format commonly used to represent structured data exchanged by APIs.

A boid might be represented as:

```json
{
  "id": 42,
  "x": 10.0,
  "y": 20.0,
  "speed": 2.5
}
```

JSON supports objects, arrays, strings, numbers, Boolean values, and `null`. It does not have a distinct integer type and decimal type in its syntax; numbers are represented using a common numeric grammar.

In ASP.NET Core, request and response data is often represented with C# classes or records. The framework can deserialize incoming JSON into an appropriate .NET type and serialize return values into JSON, depending on the endpoint configuration and result type.

For example, a request DTO might be:

```csharp
public sealed class CreateBoidRequest
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Speed { get; set; }
}
```

The DTO defines the data the client is allowed to submit. It is not necessarily the same as the application's internal `Boid` class.

Separating request and response DTOs from internal models helps prevent clients from controlling fields they should not modify, such as a server-generated identifier.

JSON itself does not enforce your application's business rules. The application still needs to validate values such as speed and determine whether the requested operation is allowed.

### 9. Content negotiation and media types

The `Content-Type` and `Accept` headers are related but have different meanings.

- `Content-Type` describes the format of the body being sent.
- `Accept` describes the response formats the client is willing to receive.

For a JSON request, a client commonly sends:

```http
Content-Type: application/json
Accept: application/json
```

The server uses these headers as part of deciding how to interpret the request and represent the response.

If the server cannot process the submitted media type, it may return `415 Unsupported Media Type`. If the server cannot provide an acceptable response format, it may return `406 Not Acceptable`, depending on its configuration and implementation.

A correctly designed API documents its supported request and response formats instead of relying on clients to guess them.

### 10. REST principles

REST stands for Representational State Transfer. It is an architectural style for designing networked systems, not a particular library or protocol implementation.

HTTP APIs are often described as REST APIs because they use HTTP methods, resource identifiers, representations, and status codes in a consistent way. However, an API using HTTP and JSON is not automatically fully RESTful.

Important REST concepts include:

- **Resources:** The system exposes identifiable concepts, such as boids or simulations.
- **Representations:** Clients exchange representations of resources, often as JSON.
- **Uniform interface:** Interactions follow consistent conventions for identifying resources and performing operations.
- **Stateless communication:** Each request contains the information needed to understand and process it. The server does not rely on hidden conversational context from a previous request.
- **Cacheability:** Responses can indicate whether they may be cached.
- **Layered system:** A client need not know whether it is communicating directly with the application server or through intermediaries.

REST also includes the concept of hypermedia-driven interaction, in which representations can provide links describing available next actions. Many APIs called REST APIs implement only a subset of the complete REST architectural constraints.

For this learning module, focus on resource-oriented routes, appropriate HTTP methods, meaningful status codes, and stateless requests. These conventions provide a solid foundation for practical ASP.NET Core APIs.

### 11. Safe methods and idempotency

HTTP methods have important behavioral properties.

A **safe method** is intended to retrieve information without requesting a change to the server's application state. `GET` is safe. A server may still record operational information, such as access logs, but the requested operation should not modify the resource state.

An **idempotent method** has the same intended effect on server state when the same request is applied one or more times.

For example, deleting the same resource repeatedly should leave the resource deleted after the first successful deletion. A later request might return `404 Not Found`, but the intended resulting state remains the same.

Common properties include:

| Method | Safe? | Idempotent? |
|---|---|---|
| `GET` | Yes | Yes |
| `POST` | No | Not guaranteed |
| `PUT` | No | Yes |
| `PATCH` | No | Not guaranteed |
| `DELETE` | No | Yes |

The idempotency of `PATCH` depends on the operation being performed. A patch that sets speed to `2.0` can be idempotent, while a patch that increments speed by `1.0` is not.

Idempotency matters when clients retry requests after network failures. If a client sends a request but never receives the response, it may not know whether the server processed the operation. Correctly designed APIs account for the possibility of repeated requests, especially for operations that create resources or trigger work.

### 12. Stateless requests

Statelessness means that each request contains the information the server needs to understand and process it, rather than relying on hidden conversational state from earlier requests.

For example, a request to retrieve boid `42` identifies that resource explicitly:

```http
GET /api/boids/42 HTTP/1.1
Accept: application/json
```

The server should not need to remember which boid the client selected in an unrelated earlier request.

Statelessness does not mean the server cannot store application data. A server can maintain boid records, simulation state, databases, caches, and other persistent information. The constraint concerns the information required to interpret an individual request.

This distinction is useful when scaling a web application across multiple server instances, because any instance can process a request when the required application state is stored in an appropriate shared or persistent system.

### 13. Error responses

A useful API communicates failures clearly and consistently.

For example, if a client requests a boid that does not exist, the server might return:

```http
HTTP/1.1 404 Not Found
Content-Type: application/json

{
  "title": "Boid not found",
  "status": 404
}
```

This is an illustrative error representation. ASP.NET Core can produce standardized Problem Details responses, which provide fields such as `title`, `status`, and `detail`.

Error responses should avoid exposing stack traces, secrets, internal paths, or other sensitive implementation details. Detailed diagnostics belong in server-side logs, while the client receives enough information to understand and correct the request where possible.

A useful distinction is between:

- **Client errors:** Invalid input, missing resources, or unauthorized operations.
- **Server errors:** Unexpected failures while processing a valid request.

Do not return `500 Internal Server Error` for every invalid request. Use a suitable client-error status when the problem is with the request, and reserve server-error statuses for server-side failures.

### 14. Testing HTTP APIs

You can test HTTP endpoints with command-line tools such as `curl`. This lets you inspect requests and responses without building a browser interface.

For example, once an ASP.NET Core application is running locally, retrieve a collection with:

```bash
curl -i http://localhost:5000/api/boids
```

The `-i` option includes the response headers, allowing you to inspect the status code and content type.

Submit JSON to create a resource with:

```bash
curl -i -X POST http://localhost:5000/api/boids \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d '{"x":10.0,"y":20.0,"speed":2.5}'
```

These commands assume the application is listening on port `5000` and exposes the specified routes. ASP.NET Core often selects a different local port, so use the URL reported by the running application.

When testing an API, check more than the returned data. Verify the status code, response headers, error behavior, and how the API handles missing or invalid input.

The key principle is that an API is a contract between the client and server. Tests should verify that the implementation follows that contract.

## Challenge

Build a small ASP.NET Core HTTP API that manages an in-memory collection of boid resources. This is a learning exercise, so the data does not need to persist after the application stops.

Your API must satisfy all of the following requirements:

1. **Create a collection endpoint:** Implement `GET /api/boids` to return all boids as JSON with an appropriate success status code.
2. **Retrieve a single resource:** Implement `GET /api/boids/{id}` to return a matching boid, or `404 Not Found` if it does not exist.
3. **Create a resource:** Implement `POST /api/boids` to accept a JSON request containing X, Y, and speed. Assign the new boid an identifier and return `201 Created` with the created resource and an appropriate resource location.
4. **Validate input:** Reject a negative speed with an appropriate client-error status. Do not create a resource when validation fails.
5. **Update a resource:** Implement `PUT /api/boids/{id}` to replace the supported fields of an existing boid. Return `404 Not Found` if the resource does not exist.
6. **Delete a resource:** Implement `DELETE /api/boids/{id}`. Return an appropriate success status when deletion succeeds and `404 Not Found` when the resource does not exist.
7. **Use resource-oriented routes:** Use the `/api/boids` collection route and `/api/boids/{id}` item route consistently. Let the HTTP method express the intended operation.
8. **Use JSON correctly:** Accept JSON request bodies and return JSON representations where a response body is appropriate.
9. **Handle query parameters:** Add a supported query parameter, such as `limit`, to the collection endpoint. Validate it and define how it affects the returned collection.
10. **Test with `curl`:** Exercise every endpoint, inspect status codes and response bodies, and verify both successful operations and failure cases.
11. **Keep the API stateless:** Each request must identify the resource or operation it needs without relying on hidden client interaction history.
12. **Keep the implementation focused:** Use a simple in-memory collection for this exercise. Do not add a database, authentication system, frontend, or unrelated infrastructure.


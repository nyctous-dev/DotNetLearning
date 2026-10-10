using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class BoidApiTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BoidApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory
            .WithWebHostBuilder(builder =>
                builder.UseEnvironment("Development"))
            .CreateClient();
    }

    [Fact]
    public async Task ExistingBoid_ReturnsOk()
    {
        using HttpResponseMessage response =
            await _client.GetAsync("/boids/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string body = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"id\":1", body);
        Assert.Contains("\"speed\":2", body);
    }

    [Fact]
    public async Task MissingBoid_ReturnsNotFoundProblem()
    {
        using HttpResponseMessage response =
            await _client.GetAsync("/boids/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        string body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Boid not found.", body);
    }

    [Fact]
    public async Task NonPositiveId_ReturnsBadRequestProblem()
    {
        using HttpResponseMessage response =
            await _client.GetAsync("/boids/0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        string body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Invalid boid identifier.", body);
    }

    [Fact]
    public async Task UnexpectedException_ReturnsSafeServerError()
    {
        using HttpResponseMessage response =
            await _client.GetAsync("/test/failure");

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        string body = await response.Content.ReadAsStringAsync();

        Assert.Contains("An unexpected error occurred.", body);
        Assert.Contains("traceId", body);
        Assert.DoesNotContain("Internal diagnostic details", body);
        Assert.DoesNotContain("InvalidOperationException", body);
        Assert.DoesNotContain(" at Program.", body);
    }
}
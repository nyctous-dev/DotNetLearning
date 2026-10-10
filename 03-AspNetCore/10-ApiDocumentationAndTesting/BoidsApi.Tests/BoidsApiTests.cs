using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public sealed class BoidsApiTests(
    WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetBoids_ReturnsExpectedData()
    {
        HttpResponseMessage response =
            await _client.GetAsync("/api/boids");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        BoidResponse[]? boids =
            await response.Content.ReadFromJsonAsync<BoidResponse[]>();

        Assert.NotNull(boids);
        Assert.Equal(2, boids.Length);
        Assert.Equal(1, boids[0].Id);
        Assert.Equal(10.0, boids[0].X);
        Assert.Equal(20.0, boids[0].Y);
        Assert.Equal(2.0, boids[0].Speed);
    }

    [Fact]
    public async Task GetUnknownBoid_ReturnsNotFound()
    {
        HttpResponseMessage response =
            await _client.GetAsync("/api/boids/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateBoid_ReturnsCreatedResource()
    {
        var request = new
        {
            X = 50.0,
            Y = 60.0,
            Speed = 4.0
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync("/api/boids", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith("/api/boids/3", response.Headers.Location!.ToString());

        BoidResponse? created =
            await response.Content.ReadFromJsonAsync<BoidResponse>();

        Assert.NotNull(created);
        Assert.Equal(3, created.Id);
        Assert.Equal(50.0, created.X);
        Assert.Equal(60.0, created.Y);
        Assert.Equal(4.0, created.Speed);
    }

    [Fact]
    public async Task CreateBoidWithNegativeSpeed_ReturnsBadRequestWithoutCreating()
    {
        BoidResponse[]? before =
            await _client.GetFromJsonAsync<BoidResponse[]>("/api/boids");

        Assert.NotNull(before);

        var request = new
        {
            X = 50.0,
            Y = 60.0,
            Speed = -1.0
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync("/api/boids", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ApiErrorResponse? error =
            await response.Content.ReadFromJsonAsync<ApiErrorResponse>();

        Assert.NotNull(error);
        Assert.Contains("speed", error.Error, StringComparison.OrdinalIgnoreCase);

        BoidResponse[]? after =
            await _client.GetFromJsonAsync<BoidResponse[]>("/api/boids");

        Assert.NotNull(after);
        Assert.Equal(before.Length, after.Length);
    }

    [Fact]
    public async Task OpenApiDocument_ContainsBoidEndpoints()
    {
        HttpResponseMessage response =
            await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument document =
            await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync());

        JsonElement paths = document.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/boids", out JsonElement boidsPath));
        Assert.True(boidsPath.TryGetProperty("get", out _));
        Assert.True(boidsPath.TryGetProperty("post", out _));

        Assert.True(
            paths.TryGetProperty("/api/boids/{id}", out JsonElement boidPath));

        Assert.True(boidPath.TryGetProperty("get", out _));
    }

    private sealed record BoidResponse(
        int Id,
        double X,
        double Y,
        double Speed);

    private sealed record ApiErrorResponse(string Error);
}
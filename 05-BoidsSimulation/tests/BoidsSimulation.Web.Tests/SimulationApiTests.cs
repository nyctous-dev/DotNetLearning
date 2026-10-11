
using System.Net;
using System.Text.Json;
using BoidsSimulation.Core.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BoidsSimulation.Web.Tests;

public sealed class SimulationApiTests
    : IClassFixture<BoidsSimulationWebApplicationFactory>
{
    private readonly BoidsSimulationWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SimulationApiTests(
        BoidsSimulationWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSimulation_ReturnsPopulationSummary()
    {
        var settings = _factory.Services
            .GetRequiredService<SimulationSettings>();

        using var response = await _client.GetAsync("/api/simulation");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var root = json.RootElement;

        Assert.Equal(
            settings.InitialBoidCount,
            root.GetProperty("boidCount").GetInt32());

        Assert.Equal(
            settings.WorldWidth,
            root.GetProperty("worldWidth").GetInt32());

        Assert.Equal(
            settings.WorldHeight,
            root.GetProperty("worldHeight").GetInt32());

        Assert.True(
            root.GetProperty("seed").TryGetInt32(out _),
            "The response must include the resolved integer seed.");
    }

    [Fact]
    public async Task GetBoid_ExistingIdReturnsPositionAndVelocity()
    {
        using var response = await _client.GetAsync("/api/boids/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var root = json.RootElement;

        Assert.Equal(1, root.GetProperty("id").GetInt32());

        var position = root.GetProperty("position");
        var velocity = root.GetProperty("velocity");

        AssertFiniteNumber(position, "x");
        AssertFiniteNumber(position, "y");
        AssertFiniteNumber(velocity, "x");
        AssertFiniteNumber(velocity, "y");
    }

    [Fact]
    public async Task GetBoid_UnknownIdReturnsNotFound()
    {
        var settings = _factory.Services
            .GetRequiredService<SimulationSettings>();

        var unknownId = settings.InitialBoidCount + 1;

        using var response = await _client.GetAsync(
            $"/api/boids/{unknownId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBoid_NonIntegerIdReturnsBadRequest()
    {
        using var response = await _client.GetAsync(
            "/api/boids/not-an-integer");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetSimulation_RepeatedRequestsKeepSamePopulationMetadata()
    {
        using var firstResponse =
            await _client.GetAsync("/api/simulation");

        using var secondResponse =
            await _client.GetAsync("/api/simulation");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        using var firstJson = await ReadJsonAsync(firstResponse);
        using var secondJson = await ReadJsonAsync(secondResponse);

        var first = firstJson.RootElement;
        var second = secondJson.RootElement;

        Assert.Equal(
            first.GetProperty("boidCount").GetInt32(),
            second.GetProperty("boidCount").GetInt32());

        Assert.Equal(
            first.GetProperty("seed").GetInt32(),
            second.GetProperty("seed").GetInt32());
    }

    private static async Task<JsonDocument> ReadJsonAsync(
        HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        return JsonDocument.Parse(content);
    }

    private static void AssertFiniteNumber(
        JsonElement parent,
        string propertyName)
    {
        var value = parent.GetProperty(propertyName).GetSingle();

        Assert.True(
            float.IsFinite(value),
            $"Expected '{propertyName}' to be finite, but got {value}.");
    }
}

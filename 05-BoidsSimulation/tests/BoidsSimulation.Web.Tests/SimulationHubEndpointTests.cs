
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BoidsSimulation.Web.Tests;

public sealed class SimulationHubEndpointTests
    : IClassFixture<BoidsSimulationWebApplicationFactory>
{
    private readonly BoidsSimulationWebApplicationFactory _factory;

    public SimulationHubEndpointTests(
        BoidsSimulationWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Negotiate_ReturnsConnectionInformation()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsync(
            "/hubs/simulation/negotiate?negotiateVersion=1",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var responseStream =
            await response.Content.ReadAsStreamAsync();

        using var document =
            await JsonDocument.ParseAsync(responseStream);

        var root = document.RootElement;

        Assert.True(
            root.TryGetProperty("connectionId", out var connectionId));

        Assert.False(
            string.IsNullOrWhiteSpace(connectionId.GetString()));

        Assert.True(
            root.TryGetProperty(
                "availableTransports",
                out var availableTransports));

        Assert.Equal(
            JsonValueKind.Array,
            availableTransports.ValueKind);

        Assert.NotEmpty(availableTransports.EnumerateArray());
    }
}

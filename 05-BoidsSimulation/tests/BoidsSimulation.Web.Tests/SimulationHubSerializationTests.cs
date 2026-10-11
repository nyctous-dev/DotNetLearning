
using System.Numerics;
using System.Text.Json;
using BoidsSimulation.Application.Snapshots;
using BoidsSimulation.Core.Simulation;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BoidsSimulation.Web.Tests;

public sealed class SimulationHubSerializationTests
    : IClassFixture<BoidsSimulationWebApplicationFactory>
{
    private readonly BoidsSimulationWebApplicationFactory _factory;

    public SimulationHubSerializationTests(
        BoidsSimulationWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void SerializeSnapshot_IncludesVectorComponents()
    {
        using var client = _factory.CreateClient();

        var serializerOptions = _factory.Services
            .GetRequiredService<IOptions<JsonHubProtocolOptions>>()
            .Value
            .PayloadSerializerOptions;

        var snapshot = SimulationSnapshot.FromBoidStates(
        [
            new BoidStateSnapshot(
                1,
                new Vector2(12.5f, 25f),
                new Vector2(3f, 4f))
        ]);

        var json = JsonSerializer.Serialize(
            snapshot,
            serializerOptions);

        using var document = JsonDocument.Parse(json);

        var boid = document.RootElement
            .GetProperty("boids")[0];

        Assert.Equal(1, boid.GetProperty("id").GetInt32());

        Assert.Equal(
            12.5f,
            boid.GetProperty("position").GetProperty("x").GetSingle());

        Assert.Equal(
            25f,
            boid.GetProperty("position").GetProperty("y").GetSingle());

        Assert.Equal(
            3f,
            boid.GetProperty("velocity").GetProperty("x").GetSingle());

        Assert.Equal(
            4f,
            boid.GetProperty("velocity").GetProperty("y").GetSingle());
    }
}

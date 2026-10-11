
using BoidsSimulation.Application.Sessions;
using BoidsSimulation.Core.Models;
using BoidsSimulation.Web.Hubs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BoidsSimulation.Web.Tests;

public sealed class SimulationHubTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SimulationHubTests(
        WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void GetSnapshot_ReturnsCurrentSimulationPopulation()
    {
        using var client = _factory.CreateClient();

        var session = _factory.Services
            .GetRequiredService<SimulationSession>();

        var settings = _factory.Services
            .GetRequiredService<SimulationSettings>();

        var hub = new SimulationHub(session);

        var snapshot = hub.GetSnapshot();

        Assert.Equal(
            settings.InitialBoidCount,
            snapshot.Boids.Count);

        Assert.All(snapshot.Boids, boid =>
        {
            Assert.True(boid.Id > 0);
            Assert.True(float.IsFinite(boid.Position.X));
            Assert.True(float.IsFinite(boid.Position.Y));
            Assert.True(float.IsFinite(boid.Velocity.X));
            Assert.True(float.IsFinite(boid.Velocity.Y));
        });

        var uniqueIds = snapshot.Boids
            .Select(boid => boid.Id)
            .Distinct()
            .Count();

        Assert.Equal(snapshot.Boids.Count, uniqueIds);
    }
}

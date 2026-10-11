
using System.Diagnostics;
using BoidsSimulation.Application.Sessions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BoidsSimulation.Web.Tests;

public sealed class HostedSimulationIntegrationTests
    : IClassFixture<BoidsSimulationWebApplicationFactory>
{
    private readonly BoidsSimulationWebApplicationFactory _factory;

    public HostedSimulationIntegrationTests(
        BoidsSimulationWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartingHost_AdvancesRegisteredSimulationSession()
    {
        using var client = _factory.CreateClient();

        var session = _factory.Services
            .GetRequiredService<SimulationSession>();

        var initialSnapshot = session.GetSnapshot();
        Assert.NotEmpty(initialSnapshot.Boids);

        var advanced = await WaitForSimulationAdvanceAsync(
            session,
            initialSnapshot,
            TimeSpan.FromSeconds(5));

        Assert.True(
            advanced,
            "The hosted simulation did not advance within five seconds.");
    }

    private static async Task<bool> WaitForSimulationAdvanceAsync(
        SimulationSession session,
        Application.Snapshots.SimulationSnapshot initialSnapshot,
        TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            await Task.Delay(50);

            var currentSnapshot = session.GetSnapshot();

            if (HasChanged(initialSnapshot, currentSnapshot))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasChanged(
        Application.Snapshots.SimulationSnapshot initial,
        Application.Snapshots.SimulationSnapshot current)
    {
        if (initial.Boids.Count != current.Boids.Count)
        {
            return true;
        }

        for (var i = 0; i < initial.Boids.Count; i++)
        {
            if (initial.Boids[i].Position != current.Boids[i].Position ||
                initial.Boids[i].Velocity != current.Boids[i].Velocity)
            {
                return true;
            }
        }

        return false;
    }
}

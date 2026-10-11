
using BoidsSimulation.Application.Settings;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Http.Connections;

namespace BoidsSimulation.Web.Tests;

public sealed class SimulationHubAuthorizationTests
    : IClassFixture<BoidsSimulationWebApplicationFactory>
{
    private readonly BoidsSimulationWebApplicationFactory _factory;

    public SimulationHubAuthorizationTests(
        BoidsSimulationWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RegularUser_CannotGetBehaviorSettings()
    {
        await using var connection = CreateConnection("User");

        await connection.StartAsync();

        await Assert.ThrowsAsync<HubException>(
            () => connection.InvokeAsync<SimulationBehaviorSettings>(
                "GetBehaviorSettings"));
    }

    [Fact]
    public async Task RegularUser_CannotUpdateBehaviorSettings()
    {
        await using var connection = CreateConnection("User");

        await connection.StartAsync();

        var attemptedSettings = new SimulationBehaviorSettings(
            25f, 50f, 50f,
            1.5f, 1.5f, 0.8f,
            250f, 100f, 2000f, 4000f, 300f);

        await Assert.ThrowsAsync<HubException>(
            () => connection.InvokeAsync<SimulationBehaviorSettings>(
                "UpdateBehaviorSettings",
                attemptedSettings));
    }

    [Fact]
    public async Task Admin_CanReadAndUpdateBehaviorSettings()
    {
        await using var connection = CreateConnection("Admin");

        await connection.StartAsync();

        var current = await connection.InvokeAsync<SimulationBehaviorSettings>(
            "GetBehaviorSettings");

        var updated = current with
        {
            SeparationRadius = current.SeparationRadius + 1f
        };

        var result = await connection.InvokeAsync<SimulationBehaviorSettings>(
            "UpdateBehaviorSettings",
            updated);

        Assert.Equal(updated.SeparationRadius, result.SeparationRadius);

        // Verify the settings store reflects the change as well.
        var currentAgain =
            await connection.InvokeAsync<SimulationBehaviorSettings>(
                "GetBehaviorSettings");

        Assert.Equal(updated.SeparationRadius, currentAgain.SeparationRadius);
    }

    private HubConnection CreateConnection(string role)
    {
        return new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/simulation", options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory =
                    _ => _factory.Server.CreateHandler();

                options.Headers["X-Test-Role"] = role;
            })
            .Build();
    }
}

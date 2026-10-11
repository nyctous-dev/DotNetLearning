using System.Numerics;
using BoidsSimulation.Application.Sessions;
using BoidsSimulation.Application.Settings;
using BoidsSimulation.Application.Snapshots;
using BoidsSimulation.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BoidsSimulation.Web.Hubs;

/// <summary>
/// Provides snapshots of the shared simulation, receives mouse interactions,
/// and exposes administrator-only runtime settings operations.
/// </summary>
public sealed class SimulationHub : Hub
{
    private readonly SimulationSession _session;

    public SimulationHub(SimulationSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    /// <summary>
    /// Returns a detached snapshot of the current shared simulation state.
    /// Guests may call this to view the simulation.
    /// </summary>
    public SimulationSnapshot GetSnapshot()
    {
        return _session.GetSnapshot();
    }

    /// <summary>
    /// Begins an attract or repel interaction for the current connection.
    /// The mode is sent as the string "Attract" or "Repel".
    /// </summary>
    [Authorize]
    public void StartInteraction(float x, float y, string mode)
    {
        if (!Enum.TryParse<BoidInteractionMode>(
                mode,
                ignoreCase: true,
                out var interactionMode) ||
            !Enum.IsDefined(interactionMode))
        {
            throw new HubException(
                "Interaction mode must be 'Attract' or 'Repel'.");
        }

        _session.SetInteraction(
            Context.ConnectionId,
            new Vector2(x, y),
            interactionMode);
    }

    /// <summary>
    /// Moves the active interaction and refreshes its timeout. The client
    /// should call this periodically while the mouse button remains held,
    /// even when the cursor has not moved.
    /// </summary>
    [Authorize]
    public void UpdateInteractionPosition(float x, float y)
    {
        _session.UpdateInteractionPosition(
            Context.ConnectionId,
            new Vector2(x, y));
    }

    /// <summary>
    /// Ends the interaction for the current connection.
    /// </summary>
    [Authorize]
    public void EndInteraction()
    {
        _session.EndInteraction(Context.ConnectionId);
    }

    /// <summary>
    /// Returns the behavior settings used by the live simulation.
    /// Only members of the Admin role may call this method.
    /// </summary>
    [Authorize(Policy = "RequireAdmin")]
    public SimulationBehaviorSettings GetBehaviorSettings()
    {
        return _session.GetBehaviorSettings();
    }

    /// <summary>
    /// Validates and applies behavior settings, then notifies every connected
    /// client. Only members of the Admin role may call this method.
    /// </summary>
    [Authorize(Policy = "RequireAdmin")]
    public async Task<SimulationBehaviorSettings> UpdateBehaviorSettings(
        SimulationBehaviorSettings behaviorSettings)
    {
        SimulationBehaviorSettings updatedSettings;

        try
        {
            updatedSettings = _session.UpdateBehaviorSettings(behaviorSettings);
        }
        catch (ArgumentException exception)
        {
            throw new HubException(
                $"Invalid simulation settings: {exception.Message}");
        }

        await Clients.All.SendAsync(
            "SimulationSettingsUpdated",
            updatedSettings);

        return updatedSettings;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _session.RemoveConnection(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}

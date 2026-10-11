
using System.Numerics;
using BoidsSimulation.Application.Settings;
using BoidsSimulation.Application.Snapshots;
using BoidsSimulation.Core.Models;
using BoidsSimulation.Core.Simulation;

namespace BoidsSimulation.Application.Sessions;

public sealed class SimulationSession
{
    private static readonly TimeSpan InteractionTimeout =
        TimeSpan.FromSeconds(1);

    private readonly object _syncRoot = new();
    private readonly SimulationState _state;
    private readonly SimulationStepper _stepper;
    private readonly SimulationSettingsStore? _settingsStore;

    private readonly Dictionary<string, ActiveInteraction> _activeInteractions =
        new(StringComparer.Ordinal);

    // Retained for existing tests and callers that do not need runtime settings.
    public SimulationSession(
        SimulationState state,
        SimulationStepper stepper)
        : this(state, stepper, null)
    {
    }

    // Dependency injection uses this constructor in the running application.
    public SimulationSession(
        SimulationState state,
        SimulationStepper stepper,
        SimulationSettingsStore? settingsStore)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(stepper);

        _state = state;
        _stepper = stepper;
        _settingsStore = settingsStore;
    }

    public SimulationSnapshot GetSnapshot()
    {
        lock (_syncRoot)
        {
            var copiedStates = _state.CopyBoidStates();

            return SimulationSnapshot.FromBoidStates(copiedStates);
        }
    }

    public SimulationBehaviorSettings GetBehaviorSettings()
    {
        lock (_syncRoot)
        {
            var settingsStore = GetRequiredSettingsStore();

            return SimulationBehaviorSettings.From(settingsStore.Current);
        }
    }

    public SimulationBehaviorSettings UpdateBehaviorSettings(
        SimulationBehaviorSettings behavior)
    {
        ArgumentNullException.ThrowIfNull(behavior);

        lock (_syncRoot)
        {
            var settingsStore = GetRequiredSettingsStore();

            // The store validates the requested values against configured limits.
            var updatedSettings = settingsStore.UpdateBehavior(behavior);

            // Apply the validated settings to the running simulation stepper.
            // The stepper recalculates its cached radii under its own lock.
            _stepper.UpdateBehaviorSettings(updatedSettings);

            return SimulationBehaviorSettings.From(updatedSettings);
        }
    }

    public void SetInteraction(
        string connectionId,
        Vector2 position,
        BoidInteractionMode mode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ValidatePosition(position);

        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        lock (_syncRoot)
        {
            _activeInteractions[connectionId] = new ActiveInteraction(
                new BoidInteraction(position, mode),
                DateTimeOffset.UtcNow);
        }
    }

    public void UpdateInteractionPosition(
        string connectionId,
        Vector2 position)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ValidatePosition(position);

        lock (_syncRoot)
        {
            if (!_activeInteractions.TryGetValue(
                    connectionId,
                    out var activeInteraction))
            {
                return;
            }

            _activeInteractions[connectionId] = activeInteraction with
            {
                Interaction = activeInteraction.Interaction with
                {
                    Position = position
                },
                LastUpdatedAtUtc = DateTimeOffset.UtcNow
            };
        }
    }

    public void EndInteraction(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        lock (_syncRoot)
        {
            _activeInteractions.Remove(connectionId);
        }
    }

    public void RemoveConnection(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        EndInteraction(connectionId);
    }

    public IReadOnlyList<BoidInteraction> GetActiveInteractions()
    {
        lock (_syncRoot)
        {
            RemoveExpiredInteractions();

            return CreateActiveInteractionSnapshot();
        }
    }

    public void Advance()
    {
        lock (_syncRoot)
        {
            RemoveExpiredInteractions();

            // Use a stable interaction snapshot for each simulation step.
            var interactions = CreateActiveInteractionSnapshot();

            _stepper.Step(_state, interactions);
        }
    }

    private SimulationSettingsStore GetRequiredSettingsStore()
    {
        return _settingsStore
            ?? throw new InvalidOperationException(
                "Runtime settings are unavailable because no " +
                "SimulationSettingsStore was provided to this session.");
    }

    private BoidInteraction[] CreateActiveInteractionSnapshot()
    {
        return _activeInteractions.Values
            .Select(active => active.Interaction)
            .ToArray();
    }

    private void RemoveExpiredInteractions()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var connectionId in _activeInteractions
                     .Where(pair =>
                         now - pair.Value.LastUpdatedAtUtc > InteractionTimeout)
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            _activeInteractions.Remove(connectionId);
        }
    }

    private static void ValidatePosition(Vector2 position)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y))
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                "Interaction coordinates must be finite.");
        }
    }

    private sealed record ActiveInteraction(
        BoidInteraction Interaction,
        DateTimeOffset LastUpdatedAtUtc);
}

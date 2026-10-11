using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

    private readonly Dictionary<string, ActiveInteraction> _activeInteractions =
        new(StringComparer.Ordinal);

    public SimulationSession(
        SimulationState state,
        SimulationStepper stepper)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(stepper);

        _state = state;
        _stepper = stepper;
    }

    public SimulationSnapshot GetSnapshot()
    {
        lock (_syncRoot)
        {
            var copiedStates = _state.CopyBoidStates();

            return SimulationSnapshot.FromBoidStates(copiedStates);
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

            // Take a stable interaction snapshot before the stepper's parallel
            // steering phase. Do not let the stepper read the mutable dictionary.
            var interactions = CreateActiveInteractionSnapshot();
            _stepper.Step(_state, interactions);
        }
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

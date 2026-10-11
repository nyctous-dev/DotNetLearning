
using BoidsSimulation.Core.Models;

namespace BoidsSimulation.Application.Settings;

/// <summary>
/// The simulation parameters administrators can adjust at runtime.
/// </summary>
public sealed record SimulationBehaviorSettings(
    float SeparationRadius,
    float AlignmentRadius,
    float CohesionRadius,
    float SeparationWeight,
    float AlignmentWeight,
    float CohesionWeight,
    float MaxSpeed,
    float MaxSteeringForce,
    float WanderStrength,
    float AttractionForce,
    float RepulsionForce,
    float InteractionRadius)
{
    public static SimulationBehaviorSettings From(
        SimulationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new SimulationBehaviorSettings(
            settings.SeparationRadius,
            settings.AlignmentRadius,
            settings.CohesionRadius,
            settings.SeparationWeight,
            settings.AlignmentWeight,
            settings.CohesionWeight,
            settings.MaxSpeed,
            settings.MaxSteeringForce,
            settings.WanderStrength,
            settings.AttractionForce,
            settings.RepulsionForce,
            settings.InteractionRadius);
    }
}

/// <summary>
/// Holds the current simulation configuration and validates runtime changes.
/// </summary>
public sealed class SimulationSettingsStore
{
    private readonly object _syncRoot = new();
    private readonly SimulationLimits _limits;
    private SimulationSettings _current;

    public SimulationSettingsStore(
        SimulationSettings initialSettings,
        SimulationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(initialSettings);
        ArgumentNullException.ThrowIfNull(limits);

        initialSettings.Validate(limits);

        _current = initialSettings;
        _limits = limits;
    }

    /// <summary>
    /// Returns an immutable snapshot of the current settings.
    /// </summary>
    public SimulationSettings Current
    {
        get
        {
            lock (_syncRoot)
            {
                return _current;
            }
        }
    }

    /// <summary>
    /// Updates adjustable behavior parameters atomically.
    /// Structural settings remain unchanged.
    /// </summary>
    public SimulationSettings UpdateBehavior(
        SimulationBehaviorSettings behavior)
    {
        ArgumentNullException.ThrowIfNull(behavior);

        lock (_syncRoot)
        {
            var updated = _current with
            {
                SeparationRadius = behavior.SeparationRadius,
                AlignmentRadius = behavior.AlignmentRadius,
                CohesionRadius = behavior.CohesionRadius,

                SeparationWeight = behavior.SeparationWeight,
                AlignmentWeight = behavior.AlignmentWeight,
                CohesionWeight = behavior.CohesionWeight,

                MaxSpeed = behavior.MaxSpeed,
                MaxSteeringForce = behavior.MaxSteeringForce,
                WanderStrength = behavior.WanderStrength,

                AttractionForce = behavior.AttractionForce,
                RepulsionForce = behavior.RepulsionForce,
                InteractionRadius = behavior.InteractionRadius
            };

            // Reject invalid values without changing the current settings.
            updated.Validate(_limits);

            _current = updated;

            return _current;
        }
    }
}
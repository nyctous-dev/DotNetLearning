
using System.Numerics;

namespace BoidsSimulation.Core.Models;

/// <summary>
/// Defines how the simulation responds to a user's mouse interaction.
/// </summary>
public enum BoidInteractionMode
{
    Attract,
    Repel
}

/// <summary>
/// Describes an active interaction at a position in world coordinates.
/// </summary>
public readonly record struct BoidInteraction(
    Vector2 Position,
    BoidInteractionMode Mode);

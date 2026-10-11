
using System.Numerics;
using BoidsSimulation.Core.Models;
using BoidsSimulation.Core.Simulation;

namespace BoidsSimulation.Core.Tests;

public sealed class SimulationStateCopyTests
{
    [Fact]
    public void CopyBoidStates_ReturnsValuesDetachedFromLiveBoids()
    {
        var originalPosition = new Vector2(10f, 20f);
        var originalVelocity = new Vector2(1f, 2f);
        var boid = new Boid(1, originalPosition, originalVelocity);
        var state = new SimulationState([boid]);

        var copiedStates = state.CopyBoidStates();

        var copiedBoid = Assert.Single(copiedStates);
        Assert.Equal(1, copiedBoid.Id);
        Assert.Equal(originalPosition, copiedBoid.Position);
        Assert.Equal(originalVelocity, copiedBoid.Velocity);

        // Mutating the live boid must not change the copied state.
        boid.Position = new Vector2(90f, 80f);
        boid.Velocity = new Vector2(-1f, -2f);

        Assert.Equal(originalPosition, copiedBoid.Position);
        Assert.Equal(originalVelocity, copiedBoid.Velocity);
    }
}

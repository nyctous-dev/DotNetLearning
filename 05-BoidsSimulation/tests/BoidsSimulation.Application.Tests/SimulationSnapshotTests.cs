using System.Numerics;
using BoidsSimulation.Application.Snapshots;
using BoidsSimulation.Core.Simulation;

namespace BoidsSimulation.Application.Tests;

public sealed class SimulationSnapshotTests
{
    [Fact]
    public void FromBoidStates_CreatesDetachedSnapshot()
    {
        var originalPosition = new Vector2(10f, 20f);
        var originalVelocity = new Vector2(1f, 2f);

        var states = new[]
        {
            new BoidStateSnapshot(
                1,
                originalPosition,
                originalVelocity)
        };

        var snapshot = SimulationSnapshot.FromBoidStates(states);

        // Replace the source array entry after snapshot creation.
        states[0] = new BoidStateSnapshot(
            1,
            new Vector2(90f, 80f),
            new Vector2(-1f, -2f));

        var snapshotBoid = Assert.Single(snapshot.Boids);

        Assert.Equal(1, snapshotBoid.Id);
        Assert.Equal(originalPosition, snapshotBoid.Position);
        Assert.Equal(originalVelocity, snapshotBoid.Velocity);
    }

    [Fact]
    public void FromBoidStates_ExposesReadOnlyBoidCollection()
    {
        var states = new[]
        {
            new BoidStateSnapshot(
                1,
                Vector2.Zero,
                Vector2.UnitX)
        };

        var snapshot = SimulationSnapshot.FromBoidStates(states);

        var boids = Assert.IsAssignableFrom<IList<BoidSnapshot>>(
            snapshot.Boids);

        Assert.Throws<NotSupportedException>(() =>
            boids[0] = new BoidSnapshot(
                2,
                Vector2.One,
                Vector2.UnitY));
    }
}
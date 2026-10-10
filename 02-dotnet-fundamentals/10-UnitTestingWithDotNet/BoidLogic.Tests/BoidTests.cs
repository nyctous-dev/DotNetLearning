using BoidLogic;
using Xunit;

namespace BoidLogic.Tests;

public sealed class BoidTests
{
    [Fact]
    public void Move_UpdatesPositionBySuppliedOffsets()
    {
        // Arrange
        Boid boid = new(id: 1, x: 10.0, y: 20.0, speed: 2.0);

        // Act
        boid.Move(offsetX: 3.0, offsetY: -4.0);

        // Assert
        Assert.Equal(13.0, boid.X);
        Assert.Equal(16.0, boid.Y);
    }

    [Fact]
    public void Move_LeavesOtherBoidUnchanged()
    {
        // Arrange
        Boid first = new(id: 1, x: 0.0, y: 0.0, speed: 1.0);
        Boid second = new(id: 2, x: 5.0, y: 8.0, speed: 2.0);

        // Act
        first.Move(2.0, 3.0);

        // Assert
        Assert.Equal(2.0, first.X);
        Assert.Equal(3.0, first.Y);
        Assert.Equal(5.0, second.X);
        Assert.Equal(8.0, second.Y);
    }

    [Fact]
    public void Constructor_RejectsNegativeSpeed()
    {
        // Act and Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Boid(
                id: 1,
                x: 0.0,
                y: 0.0,
                speed: -1.0));
    }
}
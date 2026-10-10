
namespace BoidLogic.Tests;

public sealed class GeometryTests
{
    [Theory]
    [InlineData(0.0, 0.0, 0.0, 0.0, 0.0)]
    [InlineData(0.0, 0.0, 3.0, 4.0, 5.0)]
    [InlineData(1.0, 2.0, 1.0, 7.0, 5.0)]
    [InlineData(-1.0, -1.0, 2.0, 3.0, 5.0)]
    public void CalculateDistance_ReturnsExpectedResult(
        double x1,
        double y1,
        double x2,
        double y2,
        double expected)
    {
        // Arrange and Act
        double actual = Geometry.CalculateDistance(x1, y1, x2, y2);

        // Assert
        Assert.Equal(expected, actual);
    }
}
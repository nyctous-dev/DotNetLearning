using Xunit;

public sealed class MovementServiceTests
{
    private readonly MovementService _service = new();

    [Fact]
    public void CalculateMovementSpeed_ReturnsProduct()
    {
        double result = _service.CalculateMovementSpeed(2.0, 1.5);

        Assert.Equal(3.0, result);
    }

    [Fact]
    public void CalculateMovementSpeed_RejectsNegativeBaseSpeed()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _service.CalculateMovementSpeed(-1.0, 1.5));
    }

    [Fact]
    public void CalculateMovementSpeed_RejectsNonFiniteMultiplier()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _service.CalculateMovementSpeed(2.0, double.NaN));
    }
}
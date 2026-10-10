public sealed class MovementService
{
    public double CalculateMovementSpeed(
        double baseSpeed,
        double multiplier)
    {
        ValidateSpeed(baseSpeed, nameof(baseSpeed));
        ValidateSpeed(multiplier, nameof(multiplier));

        double result = baseSpeed * multiplier;

        if (!double.IsFinite(result))
        {
            throw new OverflowException(
                "The calculated movement speed is not finite.");
        }

        return result;
    }

    private static void ValidateSpeed(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Speed values must be finite and non-negative.");
        }
    }
}
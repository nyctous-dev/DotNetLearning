public static class BoidValidation
{
    public static string? Validate(
        double x,
        double y,
        double speed)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y))
        {
            return "Coordinates must be finite numbers.";
        }

        if (!double.IsFinite(speed) || speed < 0)
        {
            return "Speed must be finite and non-negative.";
        }

        return null;
    }

    public static string? Validate(PatchBoidRequest request)
    {
        if (request.X is { } x && !double.IsFinite(x))
        {
            return "X must be a finite number.";
        }

        if (request.Y is { } y && !double.IsFinite(y))
        {
            return "Y must be a finite number.";
        }

        if (request.Speed is { } speed &&
            (!double.IsFinite(speed) || speed < 0))
        {
            return "Speed must be finite and non-negative.";
        }

        if (request.X is null &&
            request.Y is null &&
            request.Speed is null)
        {
            return "At least one field must be supplied.";
        }

        return null;
    }
}
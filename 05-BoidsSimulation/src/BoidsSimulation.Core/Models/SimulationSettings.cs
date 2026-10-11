
namespace BoidsSimulation.Core.Models;

public sealed record SimulationSettings
{
    // Missing configuration values retain invalid sentinel values.
    // Valid defaults must come from appsettings.json.

    // World configuration
    public int WorldWidth { get; init; } = int.MinValue;
    public int WorldHeight { get; init; } = int.MinValue;

    // Population configuration
    public int MaxBoids { get; init; } = int.MinValue;
    public int InitialBoidCount { get; init; } = int.MinValue;

    // Flocking behavior
    public float SeparationRadius { get; init; } = float.NaN;
    public float AlignmentRadius { get; init; } = float.NaN;
    public float CohesionRadius { get; init; } = float.NaN;

    public float SeparationWeight { get; init; } = float.NaN;
    public float AlignmentWeight { get; init; } = float.NaN;
    public float CohesionWeight { get; init; } = float.NaN;

    // Movement constraints
    public float MaxSpeed { get; init; } = float.NaN;
    public float MaxSteeringForce { get; init; } = float.NaN;

    // User interaction forces
    public float AttractionForce { get; init; } = float.NaN;
    public float RepulsionForce { get; init; } = float.NaN;
    public float InteractionRadius { get; init; } = float.NaN;

    // Simulation timing
    public float FixedTimeStep { get; init; } = float.NaN;

    public void Validate(SimulationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);

        // Reject incomplete or invalid limit configuration first.
        limits.Validate();

        ValidateInteger(
            nameof(WorldWidth), WorldWidth, limits.WorldWidth);

        ValidateInteger(
            nameof(WorldHeight), WorldHeight, limits.WorldHeight);

        ValidateInteger(
            nameof(MaxBoids), MaxBoids, limits.MaxBoids);

        ValidateInteger(
            nameof(InitialBoidCount),
            InitialBoidCount,
            limits.InitialBoidCount);

        ValidateFloat(
            nameof(SeparationRadius),
            SeparationRadius,
            limits.SeparationRadius);

        ValidateFloat(
            nameof(AlignmentRadius),
            AlignmentRadius,
            limits.AlignmentRadius);

        ValidateFloat(
            nameof(CohesionRadius),
            CohesionRadius,
            limits.CohesionRadius);

        ValidateFloat(
            nameof(SeparationWeight),
            SeparationWeight,
            limits.SeparationWeight);

        ValidateFloat(
            nameof(AlignmentWeight),
            AlignmentWeight,
            limits.AlignmentWeight);

        ValidateFloat(
            nameof(CohesionWeight),
            CohesionWeight,
            limits.CohesionWeight);

        ValidateFloat(
            nameof(MaxSpeed), MaxSpeed, limits.MaxSpeed);

        ValidateFloat(
            nameof(MaxSteeringForce),
            MaxSteeringForce,
            limits.MaxSteeringForce);

        ValidateFloat(
            nameof(AttractionForce),
            AttractionForce,
            limits.AttractionForce);

        ValidateFloat(
            nameof(RepulsionForce),
            RepulsionForce,
            limits.RepulsionForce);

        ValidateFloat(
            nameof(InteractionRadius),
            InteractionRadius,
            limits.InteractionRadius);

        ValidateFloat(
            nameof(FixedTimeStep),
            FixedTimeStep,
            limits.FixedTimeStep);

        // The startup population can never exceed the configured ceiling.
        if (InitialBoidCount > MaxBoids)
        {
            var (minimum, maximum) =
                limits.InitialBoidCount.GetBounds(
                    nameof(InitialBoidCount));

            var effectiveMaximum = Math.Min(maximum, MaxBoids);

            throw new ArgumentOutOfRangeException(
                nameof(InitialBoidCount),
                InitialBoidCount,
                $"{nameof(InitialBoidCount)} must be between " +
                $"{minimum} and {effectiveMaximum}, because it cannot " +
                $"exceed the configured MaxBoids value of {MaxBoids}.");
        }
    }

    private static void ValidateInteger(
        string settingName,
        int value,
        SimulationLimits.IntegerRange range)
    {
        var (minimum, maximum) = range.GetBounds(settingName);

        if (value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(
                settingName,
                value,
                $"{settingName} must be between {minimum} and {maximum} " +
                $"according to SimulationLimits:{settingName}.");
        }
    }

    private static void ValidateFloat(
        string settingName,
        float value,
        SimulationLimits.FloatRange range)
    {
        var (minimum, maximum) = range.GetBounds(settingName);

        if (!float.IsFinite(value) ||
            value < minimum ||
            value > maximum)
        {
            throw new ArgumentOutOfRangeException(
                settingName,
                value,
                $"{settingName} must be finite and between " +
                $"{minimum} and {maximum} according to " +
                $"SimulationLimits:{settingName}.");
        }
    }
}

public sealed record SimulationLimits
{
    public IntegerRange WorldWidth { get; init; } = null!;
    public IntegerRange WorldHeight { get; init; } = null!;

    public IntegerRange MaxBoids { get; init; } = null!;
    public IntegerRange InitialBoidCount { get; init; } = null!;

    public FloatRange SeparationRadius { get; init; } = null!;
    public FloatRange AlignmentRadius { get; init; } = null!;
    public FloatRange CohesionRadius { get; init; } = null!;

    public FloatRange SeparationWeight { get; init; } = null!;
    public FloatRange AlignmentWeight { get; init; } = null!;
    public FloatRange CohesionWeight { get; init; } = null!;

    public FloatRange MaxSpeed { get; init; } = null!;
    public FloatRange MaxSteeringForce { get; init; } = null!;

    public FloatRange AttractionForce { get; init; } = null!;
    public FloatRange RepulsionForce { get; init; } = null!;
    public FloatRange InteractionRadius { get; init; } = null!;

    public FloatRange FixedTimeStep { get; init; } = null!;

    public void Validate()
    {
        ValidateRange(WorldWidth, nameof(WorldWidth));
        ValidateRange(WorldHeight, nameof(WorldHeight));

        ValidateRange(MaxBoids, nameof(MaxBoids));
        ValidateRange(InitialBoidCount, nameof(InitialBoidCount));

        ValidateRange(SeparationRadius, nameof(SeparationRadius));
        ValidateRange(AlignmentRadius, nameof(AlignmentRadius));
        ValidateRange(CohesionRadius, nameof(CohesionRadius));

        ValidateRange(SeparationWeight, nameof(SeparationWeight));
        ValidateRange(AlignmentWeight, nameof(AlignmentWeight));
        ValidateRange(CohesionWeight, nameof(CohesionWeight));

        ValidateRange(MaxSpeed, nameof(MaxSpeed));
        ValidateRange(MaxSteeringForce, nameof(MaxSteeringForce));

        ValidateRange(AttractionForce, nameof(AttractionForce));
        ValidateRange(RepulsionForce, nameof(RepulsionForce));
        ValidateRange(InteractionRadius, nameof(InteractionRadius));

        ValidateRange(FixedTimeStep, nameof(FixedTimeStep));
    }

    private static void ValidateRange(
        IntegerRange? range,
        string settingName)
    {
        if (range is null)
        {
            throw new ArgumentException(
                $"Missing SimulationLimits:{settingName} configuration.");
        }

        _ = range.GetBounds(settingName);
    }

    private static void ValidateRange(
        FloatRange? range,
        string settingName)
    {
        if (range is null)
        {
            throw new ArgumentException(
                $"Missing SimulationLimits:{settingName} configuration.");
        }

        _ = range.GetBounds(settingName);
    }

    public sealed record IntegerRange
    {
        public int? Min { get; init; }
        public int? Max { get; init; }

        public (int Min, int Max) GetBounds(string settingName)
        {
            if (Min is not int minimum || Max is not int maximum)
            {
                throw new ArgumentException(
                    $"SimulationLimits:{settingName} must define both Min and Max.");
            }

            if (minimum > maximum)
            {
                throw new ArgumentException(
                    $"SimulationLimits:{settingName} has a minimum greater than its maximum.");
            }

            return (minimum, maximum);
        }
    }

    public sealed record FloatRange
    {
        public float? Min { get; init; }
        public float? Max { get; init; }

        public (float Min, float Max) GetBounds(string settingName)
        {
            if (Min is not float minimum ||
                Max is not float maximum ||
                !float.IsFinite(minimum) ||
                !float.IsFinite(maximum))
            {
                throw new ArgumentException(
                    $"SimulationLimits:{settingName} must define finite Min and Max values.");
            }

            if (minimum > maximum)
            {
                throw new ArgumentException(
                    $"SimulationLimits:{settingName} has a minimum greater than its maximum.");
            }

            return (minimum, maximum);
        }
    }
}

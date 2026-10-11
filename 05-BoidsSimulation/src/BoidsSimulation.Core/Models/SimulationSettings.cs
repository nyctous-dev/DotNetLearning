namespace BoidsSimulation.Core.Models;

/// <summary>
/// Configurable values that control the simulation's world and behavior.
/// </summary>
public sealed record SimulationSettings
{
    public int WorldWidth { get; init; } = int.MinValue;
    public int WorldHeight { get; init; } = int.MinValue;
    public int MaxBoids { get; init; } = int.MinValue;
    public int InitialBoidCount { get; init; } = int.MinValue;

    public float SeparationRadius { get; init; } = float.NaN;
    public float AlignmentRadius { get; init; } = float.NaN;
    public float CohesionRadius { get; init; } = float.NaN;
    public float CellSize { get; init; } = float.NaN;

    public float SeparationWeight { get; init; } = float.NaN;
    public float AlignmentWeight { get; init; } = float.NaN;
    public float CohesionWeight { get; init; } = float.NaN;

    public float MaxSpeed { get; init; } = float.NaN;
    public float MaxSteeringForce { get; init; } = float.NaN;
    public float AttractionForce { get; init; } = float.NaN;
    public float RepulsionForce { get; init; } = float.NaN;
    public float InteractionRadius { get; init; } = float.NaN;
    public float FixedTimeStep { get; init; } = float.NaN;

    public int MaxNeighborEvaluationsPerBoid { get; init; } = int.MinValue;

    /// <summary>
    /// Validates all configured simulation values against the configured limits.
    /// </summary>
    public void Validate(SimulationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);

        limits.Validate();

        ValidateInteger(nameof(WorldWidth), WorldWidth, limits.WorldWidth);
        ValidateInteger(nameof(WorldHeight), WorldHeight, limits.WorldHeight);
        ValidateInteger(nameof(MaxBoids), MaxBoids, limits.MaxBoids);
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
            nameof(CellSize),
            CellSize,
            limits.CellSize);

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

        ValidateFloat(nameof(MaxSpeed), MaxSpeed, limits.MaxSpeed);

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

        ValidateInteger(
            nameof(MaxNeighborEvaluationsPerBoid),
            MaxNeighborEvaluationsPerBoid,
            limits.MaxNeighborEvaluationsPerBoid);

        if (InitialBoidCount > MaxBoids)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxBoids),
                MaxBoids,
                $"MaxBoids ({MaxBoids}) cannot be less than " +
                $"InitialBoidCount ({InitialBoidCount}).");
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
                $"{settingName} must be between {minimum} and {maximum}, inclusive.");
        }
    }

    private static void ValidateFloat(
        string settingName,
        float value,
        SimulationLimits.FloatRange range)
    {
        var (minimum, maximum) = range.GetBounds(settingName);

        if (!float.IsFinite(value) || value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(
                settingName,
                value,
                $"{settingName} must be a finite value between " +
                $"{minimum} and {maximum}, inclusive.");
        }
    }
}

/// <summary>
/// Configured validation boundaries for the simulation settings.
/// </summary>
public sealed record SimulationLimits
{
    public IntegerRange WorldWidth { get; init; } = new();
    public IntegerRange WorldHeight { get; init; } = new();
    public IntegerRange MaxBoids { get; init; } = new();
    public IntegerRange InitialBoidCount { get; init; } = new();

    public FloatRange SeparationRadius { get; init; } = new();
    public FloatRange AlignmentRadius { get; init; } = new();
    public FloatRange CohesionRadius { get; init; } = new();
    public FloatRange CellSize { get; init; } = new();

    public FloatRange SeparationWeight { get; init; } = new();
    public FloatRange AlignmentWeight { get; init; } = new();
    public FloatRange CohesionWeight { get; init; } = new();

    public FloatRange MaxSpeed { get; init; } = new();
    public FloatRange MaxSteeringForce { get; init; } = new();
    public FloatRange AttractionForce { get; init; } = new();
    public FloatRange RepulsionForce { get; init; } = new();
    public FloatRange InteractionRadius { get; init; } = new();
    public FloatRange FixedTimeStep { get; init; } = new();

    public IntegerRange MaxNeighborEvaluationsPerBoid { get; init; } = new();

    /// <summary>
    /// Ensures every configured range has bounds and that each minimum is no
    /// greater than its maximum.
    /// </summary>
    public void Validate()
    {
        ValidateRange(nameof(WorldWidth), WorldWidth);
        ValidateRange(nameof(WorldHeight), WorldHeight);
        ValidateRange(nameof(MaxBoids), MaxBoids);
        ValidateRange(nameof(InitialBoidCount), InitialBoidCount);

        ValidateRange(nameof(SeparationRadius), SeparationRadius);
        ValidateRange(nameof(AlignmentRadius), AlignmentRadius);
        ValidateRange(nameof(CohesionRadius), CohesionRadius);
        ValidateRange(nameof(CellSize), CellSize);

        ValidateRange(nameof(SeparationWeight), SeparationWeight);
        ValidateRange(nameof(AlignmentWeight), AlignmentWeight);
        ValidateRange(nameof(CohesionWeight), CohesionWeight);

        ValidateRange(nameof(MaxSpeed), MaxSpeed);
        ValidateRange(nameof(MaxSteeringForce), MaxSteeringForce);
        ValidateRange(nameof(AttractionForce), AttractionForce);
        ValidateRange(nameof(RepulsionForce), RepulsionForce);
        ValidateRange(nameof(InteractionRadius), InteractionRadius);
        ValidateRange(nameof(FixedTimeStep), FixedTimeStep);

        ValidateRange(
            nameof(MaxNeighborEvaluationsPerBoid),
            MaxNeighborEvaluationsPerBoid);
    }

    private static void ValidateRange(
        string settingName,
        IntegerRange? range)
    {
        if (range is null)
        {
            throw new ArgumentException(
                $"Missing limits for {settingName}.",
                settingName);
        }

        _ = range.GetBounds(settingName);
    }

    private static void ValidateRange(
        string settingName,
        FloatRange? range)
    {
        if (range is null)
        {
            throw new ArgumentException(
                $"Missing limits for {settingName}.",
                settingName);
        }

        _ = range.GetBounds(settingName);
    }

    public sealed record IntegerRange
    {
        public int? Min { get; init; }
        public int? Max { get; init; }

        public (int Min, int Max) GetBounds(string settingName)
        {
            if (Min is null || Max is null)
            {
                throw new ArgumentException(
                    $"Both Min and Max must be configured for {settingName}.",
                    settingName);
            }

            if (Min.Value > Max.Value)
            {
                throw new ArgumentException(
                    $"The minimum limit for {settingName} cannot exceed its maximum.",
                    settingName);
            }

            return (Min.Value, Max.Value);
        }
    }

    public sealed record FloatRange
    {
        public float? Min { get; init; }
        public float? Max { get; init; }

        public (float Min, float Max) GetBounds(string settingName)
        {
            if (Min is null || Max is null)
            {
                throw new ArgumentException(
                    $"Both Min and Max must be configured for {settingName}.",
                    settingName);
            }

            if (!float.IsFinite(Min.Value) || !float.IsFinite(Max.Value))
            {
                throw new ArgumentException(
                    $"Limits for {settingName} must be finite values.",
                    settingName);
            }

            if (Min.Value > Max.Value)
            {
                throw new ArgumentException(
                    $"The minimum limit for {settingName} cannot exceed its maximum.",
                    settingName);
            }

            return (Min.Value, Max.Value);
        }
    }
}
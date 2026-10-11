
namespace BoidsSimulation.Core.Diagnostics;

/// <summary>
/// Timing information for one simulation tick.
/// </summary>
public sealed class FrameTimingSample
{
    public FrameTimingSample(
        TimeSpan targetDuration,
        TimeSpan actualDuration)
    {
        if (targetDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetDuration),
                "Target duration must be greater than zero.");
        }

        if (actualDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actualDuration),
                "Actual duration cannot be negative.");
        }

        TargetDuration = targetDuration;
        ActualDuration = actualDuration;
    }

    public TimeSpan TargetDuration { get; }

    public TimeSpan ActualDuration { get; }

    /// <summary>
    /// Time left in the target budget, or zero when the budget was met
    /// exactly or exceeded.
    /// </summary>
    public TimeSpan RemainingBudget =>
        ActualDuration < TargetDuration
            ? TargetDuration - ActualDuration
            : TimeSpan.Zero;

    /// <summary>
    /// Amount by which the actual duration exceeded the target budget.
    /// </summary>
    public TimeSpan Overrun =>
        ActualDuration > TargetDuration
            ? ActualDuration - TargetDuration
            : TimeSpan.Zero;

    public bool MetBudget => ActualDuration <= TargetDuration;
}

/// <summary>
/// Summary statistics for a collection of simulation tick timings.
/// </summary>
public sealed record FrameTimingSummary(
    int TickCount,
    TimeSpan AverageDuration,
    TimeSpan MaximumDuration,
    TimeSpan P95Duration,
    int MetBudgetCount,
    double MetBudgetPercentage);

/// <summary>
/// Stores timing samples and calculates performance summaries.
/// </summary>
public sealed class FrameTimingWindow
{
    private readonly List<FrameTimingSample> _samples = new();

    public int Count => _samples.Count;

    public void Add(FrameTimingSample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);
        _samples.Add(sample);
    }

    public void Clear()
    {
        _samples.Clear();
    }

    public FrameTimingSummary GetSummary()
    {
        if (_samples.Count == 0)
        {
            return new FrameTimingSummary(
                TickCount: 0,
                AverageDuration: TimeSpan.Zero,
                MaximumDuration: TimeSpan.Zero,
                P95Duration: TimeSpan.Zero,
                MetBudgetCount: 0,
                MetBudgetPercentage: 0d);
        }

        var durations = _samples
            .Select(sample => sample.ActualDuration)
            .OrderBy(duration => duration)
            .ToArray();

        var totalTicks = durations.Sum(duration => duration.Ticks);

        var averageDuration = TimeSpan.FromTicks(
            totalTicks / durations.Length);

        var maximumDuration = durations[^1];

        // Nearest-rank percentile: rank = ceiling(p * sample count).
        var p95Rank = (int)Math.Ceiling(0.95d * durations.Length);
        var p95Duration = durations[p95Rank - 1];

        var metBudgetCount = _samples.Count(sample => sample.MetBudget);

        var metBudgetPercentage =
            metBudgetCount * 100d / _samples.Count;

        return new FrameTimingSummary(
            TickCount: _samples.Count,
            AverageDuration: averageDuration,
            MaximumDuration: maximumDuration,
            P95Duration: p95Duration,
            MetBudgetCount: metBudgetCount,
            MetBudgetPercentage: metBudgetPercentage);
    }
}

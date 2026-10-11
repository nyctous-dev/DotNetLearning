
using BoidsSimulation.Core.Diagnostics;

namespace BoidsSimulation.Core.Tests;

public sealed class SimulationTimingMetricsTests
{
    [Fact]
    public void FrameTimingSample_ReportsRemainingBudget_WhenUnderBudget()
    {
        // Arrange
        var targetDuration = TimeSpan.FromMilliseconds(16);
        var actualDuration = TimeSpan.FromMilliseconds(11);

        // Act
        var sample = new FrameTimingSample(
            targetDuration,
            actualDuration);

        // Assert
        Assert.Equal(targetDuration, sample.TargetDuration);
        Assert.Equal(actualDuration, sample.ActualDuration);
        Assert.Equal(
            TimeSpan.FromMilliseconds(5),
            sample.RemainingBudget);

        Assert.Equal(TimeSpan.Zero, sample.Overrun);
        Assert.True(sample.MetBudget);
    }

    [Fact]
    public void FrameTimingSample_ReportsOverrun_WhenOverBudget()
    {
        // Arrange
        var targetDuration = TimeSpan.FromMilliseconds(16);
        var actualDuration = TimeSpan.FromMilliseconds(21);

        // Act
        var sample = new FrameTimingSample(
            targetDuration,
            actualDuration);

        // Assert
        Assert.Equal(TimeSpan.Zero, sample.RemainingBudget);
        Assert.Equal(
            TimeSpan.FromMilliseconds(5),
            sample.Overrun);

        Assert.False(sample.MetBudget);
    }

    [Fact]
    public void FrameTimingSample_ReportsNoOverrun_AtExactBudget()
    {
        // Arrange
        var targetDuration = TimeSpan.FromMilliseconds(16);

        // Act
        var sample = new FrameTimingSample(
            targetDuration,
            targetDuration);

        // Assert
        Assert.Equal(TimeSpan.Zero, sample.RemainingBudget);
        Assert.Equal(TimeSpan.Zero, sample.Overrun);
        Assert.True(sample.MetBudget);
    }

    [Fact]
    public void FrameTimingSample_RejectsZeroTargetDuration()
    {
        // Act
        var action = () => new FrameTimingSample(
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(1));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void FrameTimingSample_RejectsNegativeActualDuration()
    {
        // Act
        var action = () => new FrameTimingSample(
            TimeSpan.FromMilliseconds(16),
            TimeSpan.FromMilliseconds(-1));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void FrameTimingWindow_ReturnsEmptySummary_WhenNoSamplesExist()
    {
        // Arrange
        var window = new FrameTimingWindow();

        // Act
        var summary = window.GetSummary();

        // Assert
        Assert.Equal(0, summary.TickCount);
        Assert.Equal(TimeSpan.Zero, summary.AverageDuration);
        Assert.Equal(TimeSpan.Zero, summary.MaximumDuration);
        Assert.Equal(TimeSpan.Zero, summary.P95Duration);
        Assert.Equal(0, summary.MetBudgetCount);
        Assert.Equal(0d, summary.MetBudgetPercentage);
    }

    [Fact]
    public void FrameTimingWindow_CalculatesSummaryStatistics()
    {
        // Arrange
        var window = new FrameTimingWindow();
        var targetDuration = TimeSpan.FromMilliseconds(15);

        // Add durations from 1 ms through 20 ms.
        for (var milliseconds = 1; milliseconds <= 20; milliseconds++)
        {
            window.Add(new FrameTimingSample(
                targetDuration,
                TimeSpan.FromMilliseconds(milliseconds)));
        }

        // Act
        var summary = window.GetSummary();

        // Assert
        Assert.Equal(20, summary.TickCount);
        Assert.Equal(
            TimeSpan.FromMilliseconds(10.5),
            summary.AverageDuration);

        Assert.Equal(
            TimeSpan.FromMilliseconds(20),
            summary.MaximumDuration);

        // With nearest-rank percentile calculation, P95 of 20 sorted
        // samples is the 19th sample.
        Assert.Equal(
            TimeSpan.FromMilliseconds(19),
            summary.P95Duration);

        // Durations from 1 ms through 15 ms met the 15 ms budget.
        Assert.Equal(15, summary.MetBudgetCount);
        Assert.Equal(75d, summary.MetBudgetPercentage, 5);
    }

    [Fact]
    public void FrameTimingWindow_DoesNotChangePreviousSummary_WhenNewSampleIsAdded()
    {
        // Arrange
        var window = new FrameTimingWindow();
        var targetDuration = TimeSpan.FromMilliseconds(16);

        window.Add(new FrameTimingSample(
            targetDuration,
            TimeSpan.FromMilliseconds(10)));

        var firstSummary = window.GetSummary();

        // Act
        window.Add(new FrameTimingSample(
            targetDuration,
            TimeSpan.FromMilliseconds(20)));

        var secondSummary = window.GetSummary();

        // Assert
        Assert.Equal(1, firstSummary.TickCount);
        Assert.Equal(
            TimeSpan.FromMilliseconds(10),
            firstSummary.AverageDuration);

        Assert.Equal(2, secondSummary.TickCount);
        Assert.Equal(
            TimeSpan.FromMilliseconds(15),
            secondSummary.AverageDuration);
    }

    [Fact]
    public void FrameTimingWindow_ClearRemovesRecordedSamples()
    {
        // Arrange
        var window = new FrameTimingWindow();

        window.Add(new FrameTimingSample(
            TimeSpan.FromMilliseconds(16),
            TimeSpan.FromMilliseconds(10)));

        // Act
        window.Clear();

        var summary = window.GetSummary();

        // Assert
        Assert.Equal(0, summary.TickCount);
        Assert.Equal(TimeSpan.Zero, summary.AverageDuration);
        Assert.Equal(TimeSpan.Zero, summary.MaximumDuration);
        Assert.Equal(TimeSpan.Zero, summary.P95Duration);
        Assert.Equal(0, summary.MetBudgetCount);
        Assert.Equal(0d, summary.MetBudgetPercentage);
    }
}

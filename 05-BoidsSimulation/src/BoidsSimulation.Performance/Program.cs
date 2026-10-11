using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using BoidsSimulation.Core.Diagnostics;
using BoidsSimulation.Core.Models;
using BoidsSimulation.Core.Simulation;

namespace BoidsSimulation.Performance;

internal static class Program
{
    private sealed record SettingsDocument(
        SimulationSettings? Simulation,
        SimulationLimits? SimulationLimits);

    private sealed record BenchmarkOptions(
        int BoidCount,
        int DurationSeconds,
        int SummaryIntervalSeconds,
        int Seed,
        bool Verbose);

    private readonly record struct PhaseStatistics(
        double AverageMilliseconds,
        double MaximumMilliseconds,
        double P95Milliseconds);

    private sealed class PhaseTimingWindow
    {
        private readonly List<SimulationStepTimings> _samples = new();

        public int Count => _samples.Count;

        public void Add(SimulationStepTimings timings)
        {
            _samples.Add(timings);
        }

        public void Clear()
        {
            _samples.Clear();
        }

        public void PrintSummary()
        {
            if (_samples.Count == 0)
            {
                return;
            }

            var population = Summarize(
                sample => sample.PopulationPreparation);

            var grid = Summarize(
                sample => sample.GridRebuild);

            var steering = Summarize(
                sample => sample.SteeringCalculation);

            var velocityCommit = Summarize(
                sample => sample.VelocityCommit);

            var positionUpdate = Summarize(
                sample => sample.PositionUpdate);

            var total = Summarize(
                sample => sample.TotalDuration);

            Console.WriteLine(
                $"Phase average (ms): " +
                $"population={population.AverageMilliseconds:F3}, " +
                $"grid={grid.AverageMilliseconds:F3}, " +
                $"steering={steering.AverageMilliseconds:F3}, " +
                $"velocity-commit={velocityCommit.AverageMilliseconds:F3}, " +
                $"position-update={positionUpdate.AverageMilliseconds:F3}, " +
                $"step-total={total.AverageMilliseconds:F3}");

            Console.WriteLine(
                $"Phase P95 (ms):     " +
                $"population={population.P95Milliseconds:F3}, " +
                $"grid={grid.P95Milliseconds:F3}, " +
                $"steering={steering.P95Milliseconds:F3}, " +
                $"velocity-commit={velocityCommit.P95Milliseconds:F3}, " +
                $"position-update={positionUpdate.P95Milliseconds:F3}, " +
                $"step-total={total.P95Milliseconds:F3}");

            Console.WriteLine(
                $"Phase maximum (ms): " +
                $"population={population.MaximumMilliseconds:F3}, " +
                $"grid={grid.MaximumMilliseconds:F3}, " +
                $"steering={steering.MaximumMilliseconds:F3}, " +
                $"velocity-commit={velocityCommit.MaximumMilliseconds:F3}, " +
                $"position-update={positionUpdate.MaximumMilliseconds:F3}, " +
                $"step-total={total.MaximumMilliseconds:F3}");
        }

        public void PrintDetailedSummary()
        {
            if (_samples.Count == 0)
            {
                Console.WriteLine("No phase timings were collected.");
                return;
            }

            var population = Summarize(
                sample => sample.PopulationPreparation);

            var grid = Summarize(
                sample => sample.GridRebuild);

            var steering = Summarize(
                sample => sample.SteeringCalculation);

            var velocityCommit = Summarize(
                sample => sample.VelocityCommit);

            var positionUpdate = Summarize(
                sample => sample.PositionUpdate);

            var total = Summarize(
                sample => sample.TotalDuration);

            var instrumentedPhaseAverage =
                population.AverageMilliseconds +
                grid.AverageMilliseconds +
                steering.AverageMilliseconds +
                velocityCommit.AverageMilliseconds +
                positionUpdate.AverageMilliseconds;

            Console.WriteLine(
                $"Phase timing sample count: {_samples.Count:N0}");

            Console.WriteLine(
                $"{"Phase",-25} {"Average",10} {"P95",10} " +
                $"{"Maximum",10} {"Share",9}");

            Console.WriteLine(
                new string('-', 67));

            PrintPhase(
                "Population preparation",
                population,
                instrumentedPhaseAverage);

            PrintPhase(
                "Grid rebuild",
                grid,
                instrumentedPhaseAverage);

            PrintPhase(
                "Steering calculation",
                steering,
                instrumentedPhaseAverage);

            PrintPhase(
                "Velocity commit",
                velocityCommit,
                instrumentedPhaseAverage);

            PrintPhase(
                "Position update",
                positionUpdate,
                instrumentedPhaseAverage);

            Console.WriteLine(new string('-', 67));

            PrintPhase(
                "Total simulation step",
                total,
                total.AverageMilliseconds);

            Console.WriteLine(
                $"Sum of average phase durations: " +
                $"{instrumentedPhaseAverage:F3} ms");

            Console.WriteLine(
                $"Average measured step duration: " +
                $"{total.AverageMilliseconds:F3} ms");

            Console.WriteLine(
                "Phase shares are calculated relative to the sum of the " +
                "five instrumented phase averages. They may not total " +
                "exactly 100% due to rounding.");
        }

        private static void PrintPhase(
            string name,
            PhaseStatistics statistics,
            double phaseAverageTotal)
        {
            var share = phaseAverageTotal > 0
                ? statistics.AverageMilliseconds /
                  phaseAverageTotal * 100.0
                : 0.0;

            Console.WriteLine(
                $"{name,-25} " +
                $"{statistics.AverageMilliseconds,9:F3} " +
                $"{statistics.P95Milliseconds,9:F3} " +
                $"{statistics.MaximumMilliseconds,9:F3} " +
                $"{share,8:F1}%");
        }

        private PhaseStatistics Summarize(
            Func<SimulationStepTimings, TimeSpan> selector)
        {
            var durations = new double[_samples.Count];

            double totalMilliseconds = 0;
            double maximumMilliseconds = 0;

            for (var i = 0; i < _samples.Count; i++)
            {
                var milliseconds =
                    selector(_samples[i]).TotalMilliseconds;

                durations[i] = milliseconds;
                totalMilliseconds += milliseconds;

                if (milliseconds > maximumMilliseconds)
                {
                    maximumMilliseconds = milliseconds;
                }
            }

            Array.Sort(durations);

            var p95Index = Math.Clamp(
                (int)Math.Ceiling(durations.Length * 0.95) - 1,
                0,
                durations.Length - 1);

            return new PhaseStatistics(
                AverageMilliseconds:
                    totalMilliseconds / durations.Length,
                MaximumMilliseconds:
                    maximumMilliseconds,
                P95Milliseconds:
                    durations[p95Index]);
        }
    }

    public static int Main(string[] args)
    {
        try
        {
            if (args.Contains("--help", StringComparer.Ordinal) ||
                args.Contains("-h", StringComparer.Ordinal))
            {
                PrintUsage();
                return 0;
            }

            var (settings, limits) = LoadSettings();

            var options = ParseArguments(
                args,
                settings.InitialBoidCount,
                settings.MaxBoids);

            settings.Validate(limits);

            if (options.BoidCount > settings.MaxBoids)
            {
                throw new ArgumentException(
                    $"The requested boid count ({options.BoidCount:N0}) " +
                    $"exceeds the configured maximum " +
                    $"({settings.MaxBoids:N0}).");
            }

            RunBenchmark(settings, limits, options);

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Error: {exception.Message}");
            Console.Error.WriteLine(
                "Use --help to see the available options.");

            return 1;
        }
    }

    private static void RunBenchmark(
        SimulationSettings settings,
        SimulationLimits limits,
        BenchmarkOptions options)
    {
        var boids = CreatePopulation(
            settings,
            options.BoidCount,
            options.Seed);

        var state = new SimulationState(boids);
        var stepper = new SimulationStepper(settings, limits);

        var targetDuration =
            TimeSpan.FromSeconds(settings.FixedTimeStep);

        // These two windows are cleared after each interval.
        var frameTimingWindow = new FrameTimingWindow();
        var phaseTimingWindow = new PhaseTimingWindow();

        // These windows retain every sample for the final full-run summary.
        var overallFrameTimingWindow = new FrameTimingWindow();
        var overallPhaseTimingWindow = new PhaseTimingWindow();

        var benchmarkStart = Stopwatch.GetTimestamp();
        var summaryWindowStart = benchmarkStart;

        var nextSummaryTimestamp =
            benchmarkStart +
            (long)(options.SummaryIntervalSeconds *
                   (double)Stopwatch.Frequency);

        var totalTicks = 0L;
        var windowTicks = 0;

        var lastActualDuration = TimeSpan.Zero;

        Console.WriteLine("Boids simulation performance benchmark");
        Console.WriteLine($"Boids:             {options.BoidCount:N0}");
        Console.WriteLine(
            $"Duration:          {options.DurationSeconds:N0} seconds");
        Console.WriteLine(
            $"Target timestep:   {targetDuration.TotalMilliseconds:F3} ms");
        Console.WriteLine(
            $"Summary interval:  {options.SummaryIntervalSeconds:N0} seconds");
        Console.WriteLine($"Random seed:       {options.Seed}");
        Console.WriteLine();

        Console.WriteLine(
            "Timing phases: population preparation, grid rebuild, " +
            "steering, velocity commit, and position update.");

        Console.WriteLine(
            "The first interval may include warm-up effects; " +
            "compare subsequent intervals.");

        Console.WriteLine();

        var benchmarkDuration =
            TimeSpan.FromSeconds(options.DurationSeconds);

        while (Stopwatch.GetElapsedTime(benchmarkStart) <
               benchmarkDuration)
        {
            var tickStart = Stopwatch.GetTimestamp();

            stepper.Step(state);

            lastActualDuration =
                Stopwatch.GetElapsedTime(tickStart);

            var timings = stepper.LastStepTimings;

            var frameSample = new FrameTimingSample(
                targetDuration,
                lastActualDuration);

            frameTimingWindow.Add(frameSample);
            overallFrameTimingWindow.Add(frameSample);

            phaseTimingWindow.Add(timings);
            overallPhaseTimingWindow.Add(timings);

            totalTicks++;
            windowTicks++;

            var now = Stopwatch.GetTimestamp();

            var shouldPrintSummary =
                options.Verbose ||
                now >= nextSummaryTimestamp;

            if (shouldPrintSummary)
            {
                PrintWindowSummary(
                    frameTimingWindow,
                    phaseTimingWindow,
                    windowTicks,
                    summaryWindowStart,
                    benchmarkStart,
                    totalTicks,
                    lastActualDuration,
                    targetDuration);

                frameTimingWindow.Clear();
                phaseTimingWindow.Clear();
                windowTicks = 0;

                summaryWindowStart = Stopwatch.GetTimestamp();

                nextSummaryTimestamp =
                    summaryWindowStart +
                    (long)(options.SummaryIntervalSeconds *
                           (double)Stopwatch.Frequency);
            }

            PaceTick(tickStart, targetDuration);
        }

        // Print any measurements left in the final incomplete interval.
        if (windowTicks > 0)
        {
            PrintWindowSummary(
                frameTimingWindow,
                phaseTimingWindow,
                windowTicks,
                summaryWindowStart,
                benchmarkStart,
                totalTicks,
                lastActualDuration,
                targetDuration);
        }

        var elapsed =
            Stopwatch.GetElapsedTime(benchmarkStart);

        PrintFinalSummary(
            overallFrameTimingWindow,
            overallPhaseTimingWindow,
            totalTicks,
            elapsed,
            targetDuration);
    }

    private static void PrintWindowSummary(
        FrameTimingWindow frameTimingWindow,
        PhaseTimingWindow phaseTimingWindow,
        int windowTicks,
        long windowStartTimestamp,
        long benchmarkStartTimestamp,
        long totalTicks,
        TimeSpan lastActualDuration,
        TimeSpan targetDuration)
    {
        if (windowTicks <= 0)
        {
            return;
        }

        var window = frameTimingWindow.GetSummary();

        var windowElapsed =
            Stopwatch.GetElapsedTime(windowStartTimestamp);

        var totalElapsed =
            Stopwatch.GetElapsedTime(benchmarkStartTimestamp);

        var windowRate =
            windowTicks / windowElapsed.TotalSeconds;

        var effectiveRate =
            totalTicks / totalElapsed.TotalSeconds;

        var remainingBudget =
            targetDuration - lastActualDuration;

        Console.WriteLine(
            $"--- Window: {windowElapsed.TotalSeconds:F2} seconds ---");

        Console.WriteLine(
            $"Ticks: {window.TickCount:N0} | " +
            $"Avg: {window.AverageDuration.TotalMilliseconds:F3} ms | " +
            $"P95: {window.P95Duration.TotalMilliseconds:F3} ms | " +
            $"Max: {window.MaximumDuration.TotalMilliseconds:F3} ms");

        Console.WriteLine(
            $"Within target budget: {window.MetBudgetPercentage:F1}% " +
            $"({window.MetBudgetCount:N0}/{window.TickCount:N0})");

        if (remainingBudget >= TimeSpan.Zero)
        {
            Console.WriteLine(
                $"Last step: {lastActualDuration.TotalMilliseconds:F3} ms | " +
                $"Budget remaining: {remainingBudget.TotalMilliseconds:F3} ms");
        }
        else
        {
            Console.WriteLine(
                $"Last step: {lastActualDuration.TotalMilliseconds:F3} ms | " +
                $"Over budget: " +
                $"{Math.Abs(remainingBudget.TotalMilliseconds):F3} ms");
        }

        Console.WriteLine(
            $"Window rate: {windowRate:F2} ticks/sec | " +
            $"Overall rate: {effectiveRate:F2} ticks/sec | " +
            $"Total ticks: {totalTicks:N0}");

        phaseTimingWindow.PrintSummary();

        Console.WriteLine();
    }

    private static void PrintFinalSummary(
        FrameTimingWindow frameTimingWindow,
        PhaseTimingWindow phaseTimingWindow,
        long totalTicks,
        TimeSpan elapsed,
        TimeSpan targetDuration)
    {
        Console.WriteLine();
        Console.WriteLine("========== Final Benchmark Summary ==========");

        if (totalTicks == 0)
        {
            Console.WriteLine("No simulation steps were completed.");
            return;
        }

        var summary = frameTimingWindow.GetSummary();

        var effectiveRate =
            totalTicks / elapsed.TotalSeconds;

        var averageWallClockMilliseconds =
            elapsed.TotalMilliseconds / totalTicks;

        Console.WriteLine($"Elapsed time:                  {elapsed.TotalSeconds:F2} seconds");
        Console.WriteLine($"Completed simulation ticks:    {totalTicks:N0}");
        Console.WriteLine($"Effective tick rate:            {effectiveRate:F2} ticks/second");
        Console.WriteLine($"Target timestep:                {targetDuration.TotalMilliseconds:F3} ms");
        Console.WriteLine();

        Console.WriteLine("Overall simulation-step duration");
        Console.WriteLine(
            $"Average execution time:        " +
            $"{summary.AverageDuration.TotalMilliseconds:F3} ms");

        Console.WriteLine(
            $"P95 execution time:             " +
            $"{summary.P95Duration.TotalMilliseconds:F3} ms");

        Console.WriteLine(
            $"Maximum execution time:         " +
            $"{summary.MaximumDuration.TotalMilliseconds:F3} ms");

        Console.WriteLine(
            $"Average wall-clock time/tick:   " +
            $"{averageWallClockMilliseconds:F3} ms " +
            "(includes benchmark pacing)");

        Console.WriteLine(
            $"Ticks within target budget:     " +
            $"{summary.MetBudgetPercentage:F1}% " +
            $"({summary.MetBudgetCount:N0}/{summary.TickCount:N0})");

        Console.WriteLine();

        Console.WriteLine("Average timing by phase (milliseconds)");
        phaseTimingWindow.PrintDetailedSummary();

        Console.WriteLine("=============================================");
    }

    private static void PaceTick(
        long tickStartTimestamp,
        TimeSpan targetDuration)
    {
        var deadline =
            tickStartTimestamp +
            (long)(targetDuration.TotalSeconds * Stopwatch.Frequency);

        while (true)
        {
            var remainingTicks =
                deadline - Stopwatch.GetTimestamp();

            if (remainingTicks <= 0)
            {
                return;
            }

            var remainingSeconds =
                remainingTicks / (double)Stopwatch.Frequency;

            var remaining =
                TimeSpan.FromSeconds(remainingSeconds);

            if (remaining > TimeSpan.FromMilliseconds(2))
            {
                var sleepMilliseconds =
                    Math.Max(
                        1,
                        (int)remaining.TotalMilliseconds - 1);

                Thread.Sleep(sleepMilliseconds);
            }
            else
            {
                Thread.SpinWait(32);
            }
        }
    }

    private static Boid[] CreatePopulation(
        SimulationSettings settings,
        int boidCount,
        int seed)
    {
        var random = new Random(seed);
        var boids = new Boid[boidCount];

        var halfWidth = settings.WorldWidth / 2f;
        var halfHeight = settings.WorldHeight / 2f;

        for (var i = 0; i < boidCount; i++)
        {
            var position = new Vector2(
                (float)(random.NextDouble() * settings.WorldWidth) -
                halfWidth,
                (float)(random.NextDouble() * settings.WorldHeight) -
                halfHeight);

            var angle =
                (float)(random.NextDouble() * Math.PI * 2.0);

            var speed =
                (float)random.NextDouble() * settings.MaxSpeed;

            var velocity = new Vector2(
                MathF.Cos(angle) * speed,
                MathF.Sin(angle) * speed);

            boids[i] = new Boid(
                id: i + 1,
                position: position,
                velocity: velocity);
        }

        return boids;
    }

    private static (
        SimulationSettings Settings,
        SimulationLimits Limits) LoadSettings()
    {
        var settingsPath = FindAppSettingsPath();

        var json = File.ReadAllText(settingsPath);

        var document = JsonSerializer.Deserialize<SettingsDocument>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (document?.Simulation is null)
        {
            throw new InvalidOperationException(
                $"The Simulation section was not found in '{settingsPath}'.");
        }

        if (document.SimulationLimits is null)
        {
            throw new InvalidOperationException(
                $"The SimulationLimits section was not found in '{settingsPath}'.");
        }

        document.Simulation.Validate(document.SimulationLimits);

        return (
            document.Simulation,
            document.SimulationLimits);
    }

    private static string FindAppSettingsPath()
    {
        foreach (var startPath in new[]
                 {
                     Environment.CurrentDirectory,
                     AppContext.BaseDirectory
                 })
        {
            var directory = new DirectoryInfo(startPath);

            while (directory is not null)
            {
                var projectSettingsPath = Path.Combine(
                    directory.FullName,
                    "src",
                    "BoidsSimulation.Web",
                    "appsettings.json");

                if (File.Exists(projectSettingsPath))
                {
                    return projectSettingsPath;
                }

                directory = directory.Parent;
            }
        }

        var outputSettingsPath = Path.Combine(
            AppContext.BaseDirectory,
            "appsettings.json");

        if (File.Exists(outputSettingsPath))
        {
            return outputSettingsPath;
        }

        throw new FileNotFoundException(
            "Could not locate appsettings.json. Run this command from " +
            "the solution directory, or copy the Web project's appsettings.json " +
            "into the performance project's output directory.");
    }

    private static BenchmarkOptions ParseArguments(
        string[] args,
        int defaultBoidCount,
        int maxBoids)
    {
        var boidCount = defaultBoidCount;
        var durationSeconds = 10;
        var summaryIntervalSeconds = 1;
        var seed = 12345;
        var verbose = false;

        for (var i = 0; i < args.Length; i++)
        {
            var argument = args[i];

            switch (argument)
            {
                case "--boids":
                    boidCount = ParseIntOption(
                        args,
                        ref i,
                        "--boids",
                        minimum: 1,
                        maximum: maxBoids);
                    break;

                case "--seconds":
                    durationSeconds = ParseIntOption(
                        args,
                        ref i,
                        "--seconds",
                        minimum: 1,
                        maximum: int.MaxValue);
                    break;

                case "--summary-interval":
                    summaryIntervalSeconds = ParseIntOption(
                        args,
                        ref i,
                        "--summary-interval",
                        minimum: 1,
                        maximum: int.MaxValue);
                    break;

                case "--seed":
                    seed = ParseIntOption(
                        args,
                        ref i,
                        "--seed",
                        minimum: int.MinValue,
                        maximum: int.MaxValue);
                    break;

                case "--verbose":
                    verbose = true;
                    break;

                default:
                    throw new ArgumentException(
                        $"Unknown argument '{argument}'.");
            }
        }

        return new BenchmarkOptions(
            boidCount,
            durationSeconds,
            summaryIntervalSeconds,
            seed,
            verbose);
    }

    private static int ParseIntOption(
        string[] args,
        ref int index,
        string optionName,
        int minimum,
        int maximum)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException(
                $"Option '{optionName}' requires an integer value.");
        }

        var valueText = args[++index];

        if (!int.TryParse(
                valueText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var value))
        {
            throw new ArgumentException(
                $"Value '{valueText}' for '{optionName}' is not a valid integer.");
        }

        if (value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(
                optionName,
                value,
                $"Value must be between {minimum} and {maximum}.");
        }

        return value;
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            """
            BoidsSimulation.Performance

            Options:
              --boids N               Number of boids (default: configured InitialBoidCount)
              --seconds N             Benchmark duration in seconds (default: 10)
              --summary-interval N    Summary interval in seconds (default: 1)
              --seed N                Random seed (default: 12345)
              --verbose               Print a summary for every tick
              --help                  Show this help
            """);
    }
}
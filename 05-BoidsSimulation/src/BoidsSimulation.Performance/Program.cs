
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using BoidsSimulation.Core.Diagnostics;
using BoidsSimulation.Core.Models;
using BoidsSimulation.Core.Simulation;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.Any(arg => arg is "--help" or "-h"))
            {
                PrintUsage();
                return 0;
            }

            var options = ParseOptions(args);
            return Run(options);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Error: {exception.Message}");
            return 1;
        }
    }

    private static int Run(RunnerOptions options)
    {
        var (configuredSettings, limits, configPath) = LoadConfiguration();

        var settings = configuredSettings with
        {
            InitialBoidCount = options.BoidCount
                ?? configuredSettings.InitialBoidCount
        };

        settings.Validate(limits);

        var population = CreatePopulation(
            settings,
            settings.InitialBoidCount,
            options.Seed);

        var state = new SimulationState(population);
        var stepper = new SimulationStepper(settings, limits);

        var targetDuration = TimeSpan.FromSeconds(
            settings.FixedTimeStep);

        var runDuration = TimeSpan.FromSeconds(options.Seconds);

        Console.WriteLine("Boids Simulation Performance Runner");
        Console.WriteLine("-----------------------------------");
        Console.WriteLine($"Configuration: {configPath}");
        Console.WriteLine($"Boids:         {state.BoidCount:N0}");
        Console.WriteLine($"World:         {settings.WorldWidth:N0} x {settings.WorldHeight:N0}");
        Console.WriteLine($"Cell size:     {settings.CellSize}");
        Console.WriteLine($"Neighbor cap:  {settings.MaxNeighborEvaluationsPerBoid}");
        Console.WriteLine($"Target budget: {targetDuration.TotalMilliseconds:F3} ms/tick");
        Console.WriteLine($"Target rate:   {1d / settings.FixedTimeStep:F2} ticks/second");
        Console.WriteLine($"Run duration:  {options.Seconds:F1} seconds");
        Console.WriteLine($"Seed:          {options.Seed}");
        Console.WriteLine($"Per-tick log:  {(options.Verbose ? "enabled" : "disabled")}");
        Console.WriteLine();
        Console.WriteLine("Press Ctrl+C to stop.");
        Console.WriteLine();

        using var cancellationSource = new CancellationTokenSource();

        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };

        Console.CancelKeyPress += cancelHandler;

        var totalTimer = Stopwatch.StartNew();
        var summaryTimer = Stopwatch.StartNew();

        var timingWindow = new FrameTimingWindow();
        FrameTimingSample? lastSample = null;
        long totalTicks = 0;

        try
        {
            while (!cancellationSource.IsCancellationRequested &&
                   totalTimer.Elapsed < runDuration)
            {
                // Measure simulation work separately from waiting and logging.
                var tickStart = Stopwatch.GetTimestamp();

                stepper.Step(state);

                var stepDuration = Stopwatch.GetElapsedTime(tickStart);

                var sample = new FrameTimingSample(
                    targetDuration,
                    stepDuration);

                timingWindow.Add(sample);
                lastSample = sample;
                totalTicks++;

                if (options.Verbose)
                {
                    PrintTick(totalTicks, sample);
                }

                // Wait for the target frame deadline if the simulation
                // finished early. An over-budget tick is not hidden.
                PaceUntilDeadline(
                    tickStart,
                    targetDuration,
                    cancellationSource.Token);

                if (summaryTimer.Elapsed.TotalSeconds >=
                    options.SummaryIntervalSeconds)
                {
                    PrintSummary(
                        timingWindow.GetSummary(),
                        lastSample,
                        summaryTimer.Elapsed,
                        targetDuration,
                        totalTicks,
                        totalTimer.Elapsed);

                    timingWindow.Clear();
                    summaryTimer.Restart();
                }
            }
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }

        if (timingWindow.Count > 0 && lastSample is not null)
        {
            PrintSummary(
                timingWindow.GetSummary(),
                lastSample,
                summaryTimer.Elapsed,
                targetDuration,
                totalTicks,
                totalTimer.Elapsed);
        }

        Console.WriteLine();
        Console.WriteLine("Run finished.");
        Console.WriteLine($"Total ticks: {totalTicks:N0}");
        Console.WriteLine($"Elapsed:     {totalTimer.Elapsed.TotalSeconds:F2} seconds");

        if (totalTimer.Elapsed.TotalSeconds > 0)
        {
            Console.WriteLine(
                $"Effective rate: {totalTicks / totalTimer.Elapsed.TotalSeconds:F2} ticks/second");
        }

        return 0;
    }

    private static void PrintTick(
        long tickNumber,
        FrameTimingSample sample)
    {
        Console.WriteLine(
            $"Tick {tickNumber,8:N0} | " +
            $"Step {sample.ActualDuration.TotalMilliseconds,8:F3} ms | " +
            $"Budget {sample.TargetDuration.TotalMilliseconds,7:F3} ms | " +
            $"Remaining {sample.RemainingBudget.TotalMilliseconds,7:F3} ms | " +
            $"Overrun {sample.Overrun.TotalMilliseconds,7:F3} ms");
    }

    private static void PrintSummary(
        FrameTimingSummary summary,
        FrameTimingSample? lastSample,
        TimeSpan windowDuration,
        TimeSpan targetDuration,
        long totalTicks,
        TimeSpan totalDuration)
    {
        if (summary.TickCount == 0)
        {
            return;
        }

        var windowRate = windowDuration.TotalSeconds > 0
            ? summary.TickCount / windowDuration.TotalSeconds
            : 0;

        Console.WriteLine();
        Console.WriteLine(
            $"--- Summary: last {windowDuration.TotalSeconds:F1} seconds, " +
            $"{summary.TickCount:N0} ticks ---");

        Console.WriteLine(
            $"Target budget: {targetDuration.TotalMilliseconds:F3} ms");

        Console.WriteLine(
            $"Step duration: avg {summary.AverageDuration.TotalMilliseconds:F3} ms | " +
            $"P95 {summary.P95Duration.TotalMilliseconds:F3} ms | " +
            $"max {summary.MaximumDuration.TotalMilliseconds:F3} ms");

        Console.WriteLine(
            $"Within budget: {summary.MetBudgetCount:N0}/{summary.TickCount:N0} " +
            $"({summary.MetBudgetPercentage:F1}%)");

        if (lastSample is not null)
        {
            Console.WriteLine(
                $"Last tick: remaining {lastSample.RemainingBudget.TotalMilliseconds:F3} ms | " +
                $"overrun {lastSample.Overrun.TotalMilliseconds:F3} ms");
        }

        Console.WriteLine(
            $"Window rate: {windowRate:F2} ticks/second | " +
            $"Total: {totalTicks:N0} ticks in {totalDuration.TotalSeconds:F1} seconds");

        Console.WriteLine();
    }

    private static void PaceUntilDeadline(
        long tickStart,
        TimeSpan targetDuration,
        CancellationToken cancellationToken)
    {
        var remaining = targetDuration -
                        Stopwatch.GetElapsedTime(tickStart);

        // Sleep for most of the remaining time, then spin briefly to reduce
        // the timing error introduced by coarse sleep intervals.
        if (remaining > TimeSpan.FromMilliseconds(2))
        {
            Thread.Sleep(
                remaining - TimeSpan.FromMilliseconds(1));
        }

        while (!cancellationToken.IsCancellationRequested &&
               Stopwatch.GetElapsedTime(tickStart) < targetDuration)
        {
            Thread.SpinWait(32);
        }
    }

    private static Boid[] CreatePopulation(
        SimulationSettings settings,
        int count,
        int seed)
    {
        var random = new Random(seed);
        var boids = new Boid[count];

        for (var i = 0; i < count; i++)
        {
            var x = (float)(random.NextDouble() * settings.WorldWidth)
                    - settings.WorldWidth / 2f;

            var y = (float)(random.NextDouble() * settings.WorldHeight)
                    - settings.WorldHeight / 2f;

            var angle = random.NextDouble() * Math.PI * 2;
            var speed = (float)(random.NextDouble() * settings.MaxSpeed);

            var velocity = new Vector2(
                (float)Math.Cos(angle) * speed,
                (float)Math.Sin(angle) * speed);

            boids[i] = new Boid(
                i + 1,
                new Vector2(x, y),
                velocity);
        }

        return boids;
    }

    private static (
        SimulationSettings Settings,
        SimulationLimits Limits,
        string Path) LoadConfiguration()
    {
        var configPath = FindAppSettingsPath();
        var json = File.ReadAllText(configPath);

        var document = JsonSerializer.Deserialize<ConfigurationDocument>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (document?.Simulation is null ||
            document.SimulationLimits is null)
        {
            throw new InvalidOperationException(
                "appsettings.json must contain both Simulation and SimulationLimits sections.");
        }

        return (
            document.Simulation,
            document.SimulationLimits,
            configPath);
    }

    private static string FindAppSettingsPath()
    {
        var startingPaths = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var startingPath in startingPaths)
        {
            var directory = new DirectoryInfo(startingPath);

            while (directory is not null)
            {
                var fromRepositoryRoot = Path.Combine(
                    directory.FullName,
                    "src",
                    "BoidsSimulation.Web",
                    "appsettings.json");

                if (File.Exists(fromRepositoryRoot))
                {
                    return fromRepositoryRoot;
                }

                var fromSourceDirectory = Path.Combine(
                    directory.FullName,
                    "BoidsSimulation.Web",
                    "appsettings.json");

                if (File.Exists(fromSourceDirectory))
                {
                    return fromSourceDirectory;
                }

                directory = directory.Parent;
            }
        }

        throw new FileNotFoundException(
            "Could not locate src/BoidsSimulation.Web/appsettings.json. " +
            "Run this project from within the repository.");
    }

    private static RunnerOptions ParseOptions(string[] args)
    {
        int? boidCount = null;
        var seconds = 30d;
        var summaryIntervalSeconds = 1d;
        var seed = 12345;
        var verbose = false;

        for (var i = 0; i < args.Length; i++)
        {
            string ReadValue(string optionName)
            {
                if (i + 1 >= args.Length)
                {
                    throw new ArgumentException(
                        $"Missing value for {optionName}.");
                }

                return args[++i];
            }

            switch (args[i])
            {
                case "--boids":
                    boidCount = int.Parse(
                        ReadValue("--boids"),
                        CultureInfo.InvariantCulture);

                    if (boidCount < 0)
                    {
                        throw new ArgumentOutOfRangeException(
                            "--boids",
                            "Boid count cannot be negative.");
                    }

                    break;

                case "--seconds":
                    seconds = double.Parse(
                        ReadValue("--seconds"),
                        CultureInfo.InvariantCulture);

                    if (!double.IsFinite(seconds) || seconds <= 0)
                    {
                        throw new ArgumentOutOfRangeException(
                            "--seconds",
                            "Duration must be finite and greater than zero.");
                    }

                    break;

                case "--summary-interval":
                    summaryIntervalSeconds = double.Parse(
                        ReadValue("--summary-interval"),
                        CultureInfo.InvariantCulture);

                    if (!double.IsFinite(summaryIntervalSeconds) ||
                        summaryIntervalSeconds <= 0)
                    {
                        throw new ArgumentOutOfRangeException(
                            "--summary-interval",
                            "Summary interval must be finite and greater than zero.");
                    }

                    break;

                case "--seed":
                    seed = int.Parse(
                        ReadValue("--seed"),
                        CultureInfo.InvariantCulture);
                    break;

                case "--verbose":
                    verbose = true;
                    break;

                default:
                    throw new ArgumentException(
                        $"Unknown argument: {args[i]}");
            }
        }

        return new RunnerOptions(
            boidCount,
            seconds,
            summaryIntervalSeconds,
            seed,
            verbose);
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            """
            BoidsSimulation.Performance options:

              --boids N               Population size (defaults to appsettings)
              --seconds N             Run duration in seconds (default: 30)
              --summary-interval N    Summary interval in seconds (default: 1)
              --seed N                Random seed (default: 12345)
              --verbose               Print timing for every tick
              --help                  Show this help
            """);
    }

    private sealed record RunnerOptions(
        int? BoidCount,
        double Seconds,
        double SummaryIntervalSeconds,
        int Seed,
        bool Verbose);

    private sealed record ConfigurationDocument
    {
        public SimulationSettings? Simulation { get; init; }

        public SimulationLimits? SimulationLimits { get; init; }
    }
}

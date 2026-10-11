
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using BoidsSimulation.Core.Models;

namespace BoidsSimulation.Core.Tests;

public sealed class SimulationSettingsTests
{
    private sealed record TestConfiguration(
        SimulationSettings Settings,
        SimulationLimits Limits);

    private static IConfigurationRoot BuildConfiguration(
        IEnumerable<KeyValuePair<string, string?>>? overrides = null)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(
                "appsettings.json",
                optional: false,
                reloadOnChange: false);

        if (overrides is not null)
        {
            builder.AddInMemoryCollection(overrides);
        }

        return builder.Build();
    }

    private static TestConfiguration LoadConfiguration()
    {
        var configuration = BuildConfiguration();

        var settings = configuration
            .GetSection("Simulation")
            .Get<SimulationSettings>()
            ?? throw new InvalidOperationException(
                "Missing Simulation configuration.");

        var limits = configuration
            .GetSection("SimulationLimits")
            .Get<SimulationLimits>()
            ?? throw new InvalidOperationException(
                "Missing SimulationLimits configuration.");

        return new TestConfiguration(settings, limits);
    }

    [Fact]
    public void Appsettings_Defaults_AreValid()
    {
        var configuration = LoadConfiguration();

        configuration.Settings.Validate(configuration.Limits);
    }
    
    [Theory]
    [InlineData("CellSize")]
    [InlineData("MaxNeighborEvaluationsPerBoid")]
    [InlineData("WanderStrength")]
    public void NewSettings_AreBoundFromAppsettingsAndWithinConfiguredLimits(
        string settingName)
    {
        var configuration = LoadConfiguration();

        var settingProperty =
            typeof(SimulationSettings).GetProperty(settingName);

        Assert.NotNull(settingProperty);

        var configuredValue =
            settingProperty.GetValue(configuration.Settings);

        Assert.NotNull(configuredValue);

        var limitProperty =
            typeof(SimulationLimits).GetProperty(settingName);

        Assert.NotNull(limitProperty);

        var range = limitProperty.GetValue(configuration.Limits);

        Assert.NotNull(range);

        if (range is SimulationLimits.IntegerRange integerRange)
        {
            var (minimum, maximum) = integerRange.GetBounds(settingName);
            var value = Assert.IsType<int>(configuredValue);

            Assert.InRange(value, minimum, maximum);
            return;
        }

        if (range is SimulationLimits.FloatRange floatRange)
        {
            var (minimum, maximum) = floatRange.GetBounds(settingName);
            var value = Assert.IsType<float>(configuredValue);

            Assert.True(
                float.IsFinite(value),
                $"{settingName} must be finite.");

            Assert.InRange(value, minimum, maximum);
            return;
        }

        throw new InvalidOperationException(
            $"Unsupported limit type for setting '{settingName}'.");
    }


    public static IEnumerable<object[]> SettingBoundaryCases()
    {
        var configuration = LoadConfiguration();

        var properties = typeof(SimulationSettings)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(property => property.Name, StringComparer.Ordinal);

        foreach (var property in properties)
        {
            var rangeProperty = typeof(SimulationLimits)
                .GetProperty(property.Name)
                ?? throw new InvalidOperationException(
                    $"No matching limit exists for {property.Name}.");

            var range = rangeProperty.GetValue(configuration.Limits);

            // Verify each setting's actual configured default.
            yield return new object[]
            {
                property.Name,
                "configured default",
                property.GetValue(configuration.Settings)!,
                true
            };

            if (property.PropertyType == typeof(int) &&
                range is SimulationLimits.IntegerRange integerRange)
            {
                var (minimum, maximum) =
                    integerRange.GetBounds(property.Name);

                yield return new object[]
                {
                    property.Name, "negative", -1, false
                };

                yield return new object[]
                {
                    property.Name,
                    "zero",
                    0,
                    minimum <= 0 && maximum >= 0
                };

                if (minimum > int.MinValue)
                {
                    yield return new object[]
                    {
                        property.Name,
                        "below minimum",
                        minimum - 1,
                        false
                    };
                }

                yield return new object[]
                {
                    property.Name, "at minimum", minimum, true
                };

                yield return new object[]
                {
                    property.Name, "at maximum", maximum, true
                };

                if (maximum < int.MaxValue)
                {
                    yield return new object[]
                    {
                        property.Name,
                        "above maximum",
                        maximum + 1,
                        false
                    };
                }

                yield return new object[]
                {
                    property.Name, "integer minimum", int.MinValue, false
                };

                yield return new object[]
                {
                    property.Name, "integer maximum", int.MaxValue, false
                };
            }
            else if (property.PropertyType == typeof(float) &&
                     range is SimulationLimits.FloatRange floatRange)
            {
                var (minimum, maximum) =
                    floatRange.GetBounds(property.Name);

                yield return new object[]
                {
                    property.Name, "negative", -1f, false
                };

                yield return new object[]
                {
                    property.Name,
                    "zero",
                    0f,
                    minimum <= 0f && maximum >= 0f
                };

                yield return new object[]
                {
                    property.Name,
                    "immediately below minimum",
                    MathF.BitDecrement(minimum),
                    false
                };

                yield return new object[]
                {
                    property.Name, "at minimum", minimum, true
                };

                yield return new object[]
                {
                    property.Name, "at maximum", maximum, true
                };

                yield return new object[]
                {
                    property.Name,
                    "immediately above maximum",
                    MathF.BitIncrement(maximum),
                    false
                };

                yield return new object[]
                {
                    property.Name, "NaN", float.NaN, false
                };

                yield return new object[]
                {
                    property.Name,
                    "positive infinity",
                    float.PositiveInfinity,
                    false
                };

                yield return new object[]
                {
                    property.Name,
                    "negative infinity",
                    float.NegativeInfinity,
                    false
                };

                yield return new object[]
                {
                    property.Name, "largest finite positive float",
                    float.MaxValue, false
                };

                yield return new object[]
                {
                    property.Name, "largest finite negative float",
                    -float.MaxValue, false
                };
            }
            else
            {
                throw new InvalidOperationException(
                    $"Missing or incompatible limits for {property.Name}.");
            }
        }
    }

    [Theory]
    [MemberData(nameof(SettingBoundaryCases))]
    public void Validate_UsesConfiguredLimitsForEverySetting(
        string settingName,
        string scenario,
        object value,
        bool expectedValid)
    {
        var configuration = LoadConfiguration();

        // Keep other settings valid while isolating the setting under test.
        var baseline = configuration.Settings with
        {
            InitialBoidCount = 0
        };

        var candidate = WithSettingValue(
            baseline,
            settingName,
            value);

        // Ensure the initial population test isolates its own configured range.
        if (settingName == nameof(SimulationSettings.InitialBoidCount) &&
            value is int initialCount)
        {
            var (minimum, maximum) =
                configuration.Limits.MaxBoids.GetBounds(
                    nameof(SimulationSettings.MaxBoids));

            if (initialCount >= minimum &&
                initialCount <= maximum &&
                initialCount > candidate.MaxBoids)
            {
                candidate = candidate with
                {
                    MaxBoids = initialCount
                };
            }
        }

        var exception = Record.Exception(
            () => candidate.Validate(configuration.Limits));

        if (expectedValid)
        {
            Assert.True(
                exception is null,
                $"{settingName}, scenario '{scenario}' should be valid, " +
                $"but validation threw: {exception}");
        }
        else
        {
            Assert.True(
                exception is ArgumentOutOfRangeException,
                $"{settingName}, scenario '{scenario}' should be invalid " +
                $"with ArgumentOutOfRangeException, but got: " +
                (exception is null
                    ? "no exception"
                    : $"{exception.GetType().Name}: {exception.Message}"));
        }
    }

    private static SimulationSettings WithSettingValue(
        SimulationSettings settings,
        string settingName,
        object value)
    {
        return settingName switch
        {
            nameof(SimulationSettings.WorldWidth) =>
                settings with { WorldWidth = (int)value },

            nameof(SimulationSettings.WorldHeight) =>
                settings with { WorldHeight = (int)value },

            nameof(SimulationSettings.MaxBoids) =>
                settings with { MaxBoids = (int)value },

            nameof(SimulationSettings.InitialBoidCount) =>
                settings with { InitialBoidCount = (int)value },

            nameof(SimulationSettings.SeparationRadius) =>
                settings with { SeparationRadius = (float)value },

            nameof(SimulationSettings.AlignmentRadius) =>
                settings with { AlignmentRadius = (float)value },

            nameof(SimulationSettings.CohesionRadius) =>
                settings with { CohesionRadius = (float)value },

            nameof(SimulationSettings.SeparationWeight) =>
                settings with { SeparationWeight = (float)value },

            nameof(SimulationSettings.AlignmentWeight) =>
                settings with { AlignmentWeight = (float)value },

            nameof(SimulationSettings.CohesionWeight) =>
                settings with { CohesionWeight = (float)value },

            nameof(SimulationSettings.MaxSpeed) =>
                settings with { MaxSpeed = (float)value },

            nameof(SimulationSettings.MaxSteeringForce) =>
                settings with { MaxSteeringForce = (float)value },

            nameof(SimulationSettings.AttractionForce) =>
                settings with { AttractionForce = (float)value },

            nameof(SimulationSettings.RepulsionForce) =>
                settings with { RepulsionForce = (float)value },

            nameof(SimulationSettings.InteractionRadius) =>
                settings with { InteractionRadius = (float)value },

            nameof(SimulationSettings.FixedTimeStep) =>
                settings with { FixedTimeStep = (float)value },

            // Allows tests for planned settings to compile before their model
            // properties are added. Once present, the property is set normally.
            _ => WithSettingValueByReflection(settings, settingName, value)
        };
    }

    private static SimulationSettings WithSettingValueByReflection(
        SimulationSettings settings,
        string settingName,
        object value)
    {
        var property = typeof(SimulationSettings).GetProperty(settingName)
            ?? throw new ArgumentOutOfRangeException(
                nameof(settingName),
                settingName,
                "Unknown simulation setting.");

        var candidate = settings with
        {
            InitialBoidCount = settings.InitialBoidCount
        };

        property.SetValue(candidate, value);
        return candidate;
    }

    public static IEnumerable<object[]> SettingNames()
    {
        var propertyNames = typeof(SimulationSettings)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var propertyName in propertyNames)
        {
            yield return new object[] { propertyName };
        }

        // Run the missing-setting and invalid-binding tests for these planned
        // settings now; the reflection-based enumeration takes over once added.
        foreach (var plannedSettingName in new[]
                 {
                     "CellSize",
                     "MaxNeighborEvaluationsPerBoid"
                 })
        {
            if (!propertyNames.Contains(plannedSettingName))
            {
                yield return new object[] { plannedSettingName };
            }
        }
    }

    [Theory]
    [MemberData(nameof(SettingNames))]
    public void MissingSetting_IsRejected(string settingName)
    {
        var configuration = BuildConfiguration();

        var settings = configuration
            .GetSection("Simulation")
            .Get<SimulationSettings>()!;

        var limits = configuration
            .GetSection("SimulationLimits")
            .Get<SimulationLimits>()!;

        // Use a configuration source containing only the other settings.
        var values = typeof(SimulationSettings)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.Name != settingName)
            .ToDictionary(
                property => $"Simulation:{property.Name}",
                property => Convert.ToString(
                    property.GetValue(settings),
                    CultureInfo.InvariantCulture));

        var isolatedConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var isolatedSettings = isolatedConfiguration
            .GetSection("Simulation")
            .Get<SimulationSettings>()!;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => isolatedSettings.Validate(limits));
    }

    public static IEnumerable<object[]> InvalidSettingValues()
    {
        foreach (var setting in SettingNames())
        {
            var name = (string)setting[0];

            yield return new object[] { name, "not-a-number" };
            yield return new object[] { name, "true" };
        }
    }

    [Theory]
    [MemberData(nameof(InvalidSettingValues))]
    public void InvalidConfigurationValue_FailsBinding(
        string settingName,
        string invalidValue)
    {
        var configuration = BuildConfiguration(
            new Dictionary<string, string?>
            {
                [$"Simulation:{settingName}"] = invalidValue
            });

        Assert.Throws<InvalidOperationException>(
            () => configuration
                .GetSection("Simulation")
                .Get<SimulationSettings>());
    }

    public static IEnumerable<object[]> LimitBoundNames()
    {
        foreach (var property in typeof(SimulationLimits)
                     .GetProperties(
                         BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.PropertyType ==
                    typeof(SimulationLimits.IntegerRange) ||
                property.PropertyType ==
                    typeof(SimulationLimits.FloatRange))
            {
                yield return new object[] { property.Name, "Min" };
                yield return new object[] { property.Name, "Max" };
            }
        }
    }

    [Theory]
    [MemberData(nameof(LimitBoundNames))]
    public void MissingLimitBound_IsRejected(
        string settingName,
        string boundName)
    {
        var configuration = BuildConfiguration(
            new Dictionary<string, string?>
            {
                [$"SimulationLimits:{settingName}:{boundName}"] = null
            });

        var limits = configuration
            .GetSection("SimulationLimits")
            .Get<SimulationLimits>()!;

        Assert.Throws<ArgumentException>(limits.Validate);
    }

    [Theory]
    [MemberData(nameof(LimitBoundNames))]
    public void InvalidLimitDataType_FailsBinding(
        string settingName,
        string boundName)
    {
        var configuration = BuildConfiguration(
            new Dictionary<string, string?>
            {
                [$"SimulationLimits:{settingName}:{boundName}"] =
                    "not-a-number"
            });

        Assert.Throws<InvalidOperationException>(
            () => configuration
                .GetSection("SimulationLimits")
                .Get<SimulationLimits>());
    }

    [Fact]
    public void InvertedIntegerRange_IsRejected()
    {
        var existingLimits = LoadConfiguration().Limits;

        var (_, maximum) = existingLimits.WorldWidth.GetBounds(
            nameof(SimulationSettings.WorldWidth));

        var configuration = BuildConfiguration(
            new Dictionary<string, string?>
            {
                ["SimulationLimits:WorldWidth:Min"] =
                    checked(maximum + 1).ToString(
                        CultureInfo.InvariantCulture)
            });

        var limits = configuration
            .GetSection("SimulationLimits")
            .Get<SimulationLimits>()!;

        Assert.Throws<ArgumentException>(limits.Validate);
    }

    [Fact]
    public void InvertedFloatRange_IsRejected()
    {
        var existingLimits = LoadConfiguration().Limits;

        var (_, maximum) = existingLimits.SeparationRadius.GetBounds(
            nameof(SimulationSettings.SeparationRadius));

        var configuration = BuildConfiguration(
            new Dictionary<string, string?>
            {
                ["SimulationLimits:SeparationRadius:Min"] =
                    MathF.BitIncrement(maximum).ToString(
                        "R",
                        CultureInfo.InvariantCulture)
            });

        var limits = configuration
            .GetSection("SimulationLimits")
            .Get<SimulationLimits>()!;

        Assert.Throws<ArgumentException>(limits.Validate);
    }

    [Fact]
    public void InitialBoidCount_CannotExceedConfiguredMaxBoids()
    {
        var configuration = LoadConfiguration();

        var lowerMaximum =
            configuration.Settings.InitialBoidCount - 1;

        var (minimum, maximum) =
            configuration.Limits.MaxBoids.GetBounds(
                nameof(SimulationSettings.MaxBoids));

        Assert.InRange(lowerMaximum, minimum, maximum);
        Assert.True(
            lowerMaximum < configuration.Settings.InitialBoidCount);

        var settings = configuration.Settings with
        {
            MaxBoids = lowerMaximum
        };

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => settings.Validate(configuration.Limits));

        Assert.Contains(
            lowerMaximum.ToString(CultureInfo.InvariantCulture),
            exception.Message);
    }
}
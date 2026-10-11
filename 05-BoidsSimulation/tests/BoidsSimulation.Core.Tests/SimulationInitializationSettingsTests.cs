
using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using BoidsSimulation.Core.Models;
using BoidsSimulation.Core.Simulation;

namespace BoidsSimulation.Core.Tests;

public sealed class SimulationInitializationSettingsTests
{
    private static IConfigurationRoot BuildConfiguration(
        string? seedValue = null,
        bool includeSeed = true)
    {
        var values = new Dictionary<string, string?>();

        if (includeSeed)
        {
            values["SimulationInitialization:Seed"] = seedValue;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static SimulationInitializationSettings BindSettings(
        string? seedValue = null,
        bool includeSeed = true)
    {
        var configuration = BuildConfiguration(
            seedValue,
            includeSeed);

        return configuration
            .GetSection("SimulationInitialization")
            .Get<SimulationInitializationSettings>()
            ?? new SimulationInitializationSettings();
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-12345)]
    [InlineData(0)]
    [InlineData(12345)]
    [InlineData(int.MaxValue)]
    public void Seed_BindsConfiguredIntegerValues(int expectedSeed)
    {
        var seedValue = expectedSeed.ToString(
            CultureInfo.InvariantCulture);

        var settings = BindSettings(seedValue);

        Assert.Equal(expectedSeed, settings.Seed);
    }

    [Fact]
    public void NullSeed_IsAllowed()
    {
        var settings = BindSettings(seedValue: null);

        Assert.Null(settings.Seed);
    }

    [Fact]
    public void MissingSeed_IsAllowed()
    {
        var settings = BindSettings(
            seedValue: null,
            includeSeed: false);

        Assert.Null(settings.Seed);
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("true")]
    [InlineData("2147483648")]
    [InlineData("-2147483649")]
    public void InvalidSeedValue_FailsConfigurationBinding(
        string invalidValue)
    {
        var configuration = BuildConfiguration(invalidValue);

        Assert.Throws<InvalidOperationException>(() =>
        {
            configuration
                .GetSection("SimulationInitialization")
                .Get<SimulationInitializationSettings>();
        });
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-12345)]
    [InlineData(0)]
    [InlineData(12345)]
    [InlineData(int.MaxValue)]
    public void SeedProvider_UsesConfiguredSeed(int configuredSeed)
    {
        var provider = new SimulationSeedProvider(configuredSeed);

        Assert.Equal(configuredSeed, provider.Seed);
    }

    [Fact]
    public void SeedProvider_GeneratesSeedWhenNoneIsConfigured()
    {
        var provider = new SimulationSeedProvider(
            configuredSeed: null);

        Assert.InRange(provider.Seed, 0, int.MaxValue);
    }

    [Fact]
    public void SeedProvider_ReturnsSameGeneratedSeedForItsLifetime()
    {
        var provider = new SimulationSeedProvider(
            configuredSeed: null);

        var firstRead = provider.Seed;
        var secondRead = provider.Seed;
        var thirdRead = provider.Seed;

        Assert.Equal(firstRead, secondRead);
        Assert.Equal(firstRead, thirdRead);
    }
}

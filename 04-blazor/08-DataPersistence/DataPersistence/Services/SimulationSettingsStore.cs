using System.Text.Json;
using BlazorPersistence.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;

namespace BlazorPersistence.Services;

public sealed class SimulationSettingsStore
{
    private const string StorageKey = "boids.simulationSettings.v1";

    private readonly IJSRuntime _js;
    private readonly string _filePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public SimulationSettingsStore(
        IJSRuntime js,
        IHostEnvironment environment)
    {
        _js = js;
        _filePath = Path.Combine(
            environment.ContentRootPath,
            "data",
            "simulation-settings.json");
    }

    public async Task<SimulationSettings> LoadAsync()
    {
        SimulationSettings? browserSettings =
            await TryLoadFromBrowserAsync();

        if (browserSettings is not null)
        {
            return browserSettings;
        }

        SimulationSettings? fileSettings = await TryLoadFromFileAsync();

        return fileSettings ?? new SimulationSettings();
    }

    public async Task SaveAsync(SimulationSettings settings)
    {
        if (!settings.HasValidValues())
        {
            throw new ArgumentException(
                "The simulation settings contain invalid values.",
                nameof(settings));
        }

        string json = JsonSerializer.Serialize(settings, JsonOptions);

        string directory = Path.GetDirectoryName(_filePath)!;
        Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(_filePath, json);

        await _js.InvokeVoidAsync(
            "simulationStorage.set",
            StorageKey,
            json);
    }

    public async Task ClearAsync()
    {
        await _js.InvokeVoidAsync(
            "simulationStorage.remove",
            StorageKey);

        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }

    private async Task<SimulationSettings?> TryLoadFromBrowserAsync()
    {
        try
        {
            string? json = await _js.InvokeAsync<string?>(
                "simulationStorage.get",
                StorageKey);

            return DeserializeValidSettings(json);
        }
        catch (JSException)
        {
            // Browser storage may be unavailable. Try the JSON file instead.
            return null;
        }
    }

    private async Task<SimulationSettings?> TryLoadFromFileAsync()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return null;
            }

            string json = await File.ReadAllTextAsync(_filePath);

            return DeserializeValidSettings(json);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static SimulationSettings? DeserializeValidSettings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            SimulationSettings? settings =
                JsonSerializer.Deserialize<SimulationSettings>(json);

            return settings is not null && settings.HasValidValues()
                ? settings
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
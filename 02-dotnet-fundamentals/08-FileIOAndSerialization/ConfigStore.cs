using System.Text.Json;

public sealed class ConfigStore
{
    private readonly string _configPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public ConfigStore(string workingDirectory)
    {
        string dataDirectory = Path.Combine(workingDirectory, "data");
        Directory.CreateDirectory(dataDirectory);

        _configPath = Path.Combine(dataDirectory, "config.json");
    }

    public async Task SaveAsync(SimulationConfig config)
    {
        Validate(config);

        await using FileStream stream = File.Create(_configPath);
        await JsonSerializer.SerializeAsync(stream, config, JsonOptions);
    }

    public async Task<SimulationConfig> LoadAsync()
    {
        try
        {
            await using FileStream stream = File.OpenRead(_configPath);

            SimulationConfig? config =
                await JsonSerializer.DeserializeAsync<SimulationConfig>(stream);

            if (config is null)
            {
                Console.WriteLine("Configuration was empty; using defaults.");
                return SimulationConfig.CreateDefault();
            }

            try
            {
                Validate(config);
                return config;
            }
            catch (InvalidDataException exception)
            {
                Console.WriteLine(
                    $"Configuration is invalid: {exception.Message} Using defaults.");

                return SimulationConfig.CreateDefault();
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("Configuration file not found; using defaults.");
            return SimulationConfig.CreateDefault();
        }
        catch (JsonException exception)
        {
            Console.WriteLine(
                $"Configuration contains invalid JSON: {exception.Message} Using defaults.");

            return SimulationConfig.CreateDefault();
        }
        catch (IOException exception)
        {
            Console.WriteLine(
                $"Could not read configuration: {exception.Message} Using defaults.");

            return SimulationConfig.CreateDefault();
        }
        catch (UnauthorizedAccessException exception)
        {
            Console.WriteLine(
                $"Access to the configuration was denied: {exception.Message} Using defaults.");

            return SimulationConfig.CreateDefault();
        }
    }

    private static void Validate(SimulationConfig config)
    {
        if (config.BoidCount < 0)
        {
            throw new InvalidDataException(
                "BoidCount cannot be negative.");
        }

        if (!double.IsFinite(config.MovementSpeed) ||
            config.MovementSpeed < 0)
        {
            throw new InvalidDataException(
                "MovementSpeed must be finite and non-negative.");
        }
    }
}
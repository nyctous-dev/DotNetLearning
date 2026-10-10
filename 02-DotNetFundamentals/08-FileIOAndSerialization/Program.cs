string workingDirectory = Directory.GetCurrentDirectory();
ConfigStore store = new(workingDirectory);

SimulationConfig config = new()
{
    BoidCount = 150,
    MovementSpeed = 2.5,
    IsRunning = false
};

await store.SaveAsync(config);
Console.WriteLine("Configuration saved.");

SimulationConfig loadedConfig = await store.LoadAsync();

Console.WriteLine("Loaded configuration:");
Console.WriteLine($"Boid count: {loadedConfig.BoidCount}");
Console.WriteLine($"Movement speed: {loadedConfig.MovementSpeed}");
Console.WriteLine($"Is running: {loadedConfig.IsRunning}");
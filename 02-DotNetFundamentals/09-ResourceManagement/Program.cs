
string dataDirectory = Path.Combine(
    Directory.GetCurrentDirectory(),
    "data");

string filePath = Path.Combine(dataDirectory, "simulation.txt");

try
{
    Directory.CreateDirectory(dataDirectory);

    Console.WriteLine("=== Writing simulation data ===");
    WriteSimulationFile(filePath);
    Console.WriteLine($"Saved to: {filePath}");

    Console.WriteLine();
    Console.WriteLine("=== Reading simulation data ===");

    using SimulationFileReader reader = new(filePath);

    string? line;

    while ((line = reader.ReadLine()) is not null)
    {
        Console.WriteLine(line);
    }

    Console.WriteLine("Finished reading the file.");
}
catch (UnauthorizedAccessException exception)
{
    Console.WriteLine($"Access denied: {exception.Message}");
}
catch (IOException exception)
{
    Console.WriteLine($"File operation failed: {exception.Message}");
}

static void WriteSimulationFile(string path)
{
    using StreamWriter writer = new(path);

    writer.WriteLine("Boids Simulation");
    writer.WriteLine("Boid 1: Position=(10, 20)");
    writer.WriteLine("Boid 2: Position=(30, 40)");
    writer.WriteLine("Boid 3: Position=(50, 60)");
}
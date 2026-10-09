
List<int> boidCounts = [10, 20, 30];

string? simulationName = null;

Console.WriteLine("=== Project Structure and Configuration ===");

Console.WriteLine($"Boid counts: {string.Join(", ", boidCounts)}");
Console.WriteLine($"Number of entries: {boidCounts.Count}");

Console.WriteLine(
    $"Simulation name: {simulationName ?? "Unnamed simulation"}");


// Console.WriteLine(simulationName.Length);

if (simulationName is not null)
{
    Console.WriteLine($"Name length: {simulationName.Length}");
}
else
{
    Console.WriteLine("No simulation name has been configured.");
}

Console.WriteLine("Configuration check completed successfully.");




// .csproj file
    // <Project Sdk="Microsoft.NET.Sdk">

    // <PropertyGroup>
    //     <OutputType>Exe</OutputType>
    //     <TargetFramework>net10.0</TargetFramework>
    //     <RootNamespace>_02_ProjectStructureAndConfiguration</RootNamespace>
    //     <ImplicitUsings>enable</ImplicitUsings>
    //     <Nullable>enable</Nullable>
    // </PropertyGroup>

    // </Project>

// dotnet --list-sdks
    // 10.0.112 [/usr/lib/dotnet/sdk]

// dotnet --list-runtimes
    // Microsoft.AspNetCore.App 10.0.12 [/usr/lib/dotnet/shared/Microsoft.AspNetCore.App]
    // Microsoft.NETCore.App 10.0.12 [/usr/lib/dotnet/shared/Microsoft.NETCore.App]

// dotnet msbuild -getProperty:TargetFramework
    // net10.0

// dotnet msbuild -getProperty:OutputType
    // Exe

// dotnet build --configuration Debug
    // Restore complete (1.7s)
    // 02-ProjectStructureAndConfiguration net10.0 succeeded (1.1s) → bin/Debug/net10.0/02-ProjectStructureAndConfiguration.dll

    // Build succeeded in 4.2s

// dotnet build --configuration Release
    // Restore complete (1.8s)
    // 02-ProjectStructureAndConfiguration net10.0 succeeded (1.2s) → bin/Release/net10.0/02-ProjectStructureAndConfiguration.dll

    // Build succeeded in 4.4s

// dotnet run
    // === Project Structure and Configuration ===
    // Boid counts: 10, 20, 30
    // Number of entries: 3
    // Simulation name: Unnamed simulation
    // No simulation name has been configured.
    // Configuration check completed successfully.

// <ImplicitUsings>disable</ImplicitUsings>

// dotnet build
    // Restore complete (1.9s)
    // 02-ProjectStructureAndConfiguration net10.0 failed with 8 error(s) (0.9s)
    //     /home/mahonri/Desktop/DotNetLearning/02-dotnet-fundamentals/02-ProjectStructureAndConfiguration/Program.cs(2,1): error CS0246: The type or namespace name 'List<>' could not be found (are you missing a using directive or an assembly reference?)
    //     /home/mahonri/Desktop/DotNetLearning/02-dotnet-fundamentals/02-ProjectStructureAndConfiguration/Program.cs(6,1): error CS0103: The name 'Console' does not exist in the current context
    //     /home/mahonri/Desktop/DotNetLearning/02-dotnet-fundamentals/02-ProjectStructureAndConfiguration/Program.cs(8,1): error CS0103: The name 'Console' does not exist in the current context
    //     /home/mahonri/Desktop/DotNetLearning/02-dotnet-fundamentals/02-ProjectStructureAndConfiguration/Program.cs(9,1): error CS0103: The name 'Console' does not exist in the current context
    //     /home/mahonri/Desktop/DotNetLearning/02-dotnet-fundamentals/02-ProjectStructureAndConfiguration/Program.cs(11,1): error CS0103: The name 'Console' does not exist in the current context
    //     /home/mahonri/Desktop/DotNetLearning/02-dotnet-fundamentals/02-ProjectStructureAndConfiguration/Program.cs(16,5): error CS0103: The name 'Console' does not exist in the current context
    //     /home/mahonri/Desktop/DotNetLearning/02-dotnet-fundamentals/02-ProjectStructureAndConfiguration/Program.cs(20,5): error CS0103: The name 'Console' does not exist in the current context
    //     /home/mahonri/Desktop/DotNetLearning/02-dotnet-fundamentals/02-ProjectStructureAndConfiguration/Program.cs(23,1): error CS0103: The name 'Console' does not exist in the current context

    // Build failed with 8 error(s) in 4.3s

// <ImplicitUsings>enable</ImplicitUsings>

// dotnet build
    // Restore complete (1.7s)
    // 02-ProjectStructureAndConfiguration net10.0 succeeded (0.7s) → bin/Debug/net10.0/02-ProjectStructureAndConfiguration.dll

    // Build succeeded in 3.9s
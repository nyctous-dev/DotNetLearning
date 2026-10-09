
const int boidCount = 100;

Console.WriteLine("Boids simulation started.");
Console.WriteLine($"Boid count: {boidCount}");




// dotnet restore
    // Restore complete (1.6s)

    // Build succeeded in 2.6s

// dotnet build
    // Restore complete (1.8s)
    // 04-BuildAndExecution net10.0 succeeded (1.0s) → bin/Debug/net10.0/04-BuildAndExecution.dll

    // Build succeeded in 4.3s

// dotnet run
    // Boids simulation started.
    // Boid count: 100

// dotnet build --configuration Debug
    // Restore complete (1.9s)
    // 04-BuildAndExecution net10.0 succeeded (1.0s) → bin/Debug/net10.0/04-BuildAndExecution.dll

    // Build succeeded in 4.3s

// dotnet build --configuration Release
    // Restore complete (1.8s)
    // 04-BuildAndExecution net10.0 succeeded (1.3s) → bin/Release/net10.0/04-BuildAndExecution.dll

    // Build succeeded in 4.5s

// dotnet clean
    // Build succeeded in 1.5s

// dotnet restore
// dotnet build --configuration Release
    // Restore complete (1.8s)
    // 04-BuildAndExecution net10.0 succeeded (1.0s) → bin/Release/net10.0/04-BuildAndExecution.dll

    // Build succeeded in 4.2s

// dotnet publish --configuration Release --self-contained false
    // Restore complete (1.8s)
    // 04-BuildAndExecution net10.0 succeeded (0.9s) → bin/Release/net10.0/publish/

    // Build succeeded in 4.1s

// dotnet bin/Release/net10.0/publish/04-BuildAndExecution.dll
    // Boids simulation started.
    // Boid count: 100

// ls bin/Release/net10.0/publish
    // 04-BuildAndExecution            04-BuildAndExecution.dll  04-BuildAndExecution.runtimeconfig.json
    // 04-BuildAndExecution.deps.json  04-BuildAndExecution.pdb
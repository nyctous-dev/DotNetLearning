
using Newtonsoft.Json;

var simulation = new
{
    Name = "Boids",
    BoidCount = 100,
    IsRunning = true
};

string json = JsonConvert.SerializeObject(
    simulation,
    Formatting.Indented);

Console.WriteLine(json);



// <RestorePackagesPath>$(MSBuildThisFileDirectory).packages</RestorePackagesPath>

// dotnet add 02-dotnet-fundamentals/03-NuGetAndDependencies/03-NuGetAndDependencies.csproj package Newtonsoft.Json --version 13.0.4
    // <ItemGroup>
    //   <PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
    // </ItemGroup>

// dotnet restore 02-dotnet-fundamentals/03-NuGetAndDependencies/03-NuGetAndDependencies.csproj
    // Restore complete (1.6s)

    // Build succeeded in 2.6s

// dotnet list 02-dotnet-fundamentals/03-NuGetAndDependencies/03-NuGetAndDependencies.csproj package --include-transitive
    // Restore complete (1.7s)

    // Build succeeded in 2.8s
    // Project '03-NuGetAndDependencies' has the following package references
    // [net10.0]: 
    // Top-level Package      Requested   Resolved
    // > Newtonsoft.Json      13.0.4      13.0.4 

// dotnet build 02-dotnet-fundamentals/03-NuGetAndDependencies/03-NuGetAndDependencies.csproj
    // Restore complete (1.8s)
    // 03-NuGetAndDependencies net10.0 succeeded (1.5s) → 02-dotnet-fundamentals/03-NuGetAndDependencies/bin/Debug/net10.0/03-NuGetAndDependencies.dll

    // Build succeeded in 4.8s

// dotnet run --project 02-dotnet-fundamentals/03-NuGetAndDependencies/03-NuGetAndDependencies.csproj
    // {
    // "Name": "Boids",
    // "BoidCount": 100,
    // "IsRunning": true
    // }

// dotnet remove 02-dotnet-fundamentals/03-NuGetAndDependencies/03-NuGetAndDependencies.csproj package Newtonsoft.Json
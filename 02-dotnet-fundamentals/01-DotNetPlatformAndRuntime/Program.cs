
List<int> values = [10, 20, 30, 40, 50];

Console.WriteLine("Hello from .NET!");
Console.WriteLine($"Current date and time: {DateTime.Now}");
Console.WriteLine($"Number of values: {values.Count}");


// dotnet --info
    // .NET SDK:
    // Version:           10.0.112
    // Commit:            95017c711e
    // Workload version:  10.0.100-manifests.63b36e85
    // MSBuild version:   18.0.11+95017c711
    // ...

// dotnet --list-sdks
    // 10.0.112 [/usr/lib/dotnet/sdk]

// dotnet --list-runtimes
    // Microsoft.AspNetCore.App 10.0.12 [/usr/lib/dotnet/shared/Microsoft.AspNetCore.App]
    // Microsoft.NETCore.App 10.0.12 [/usr/lib/dotnet/shared/Microsoft.NETCore.App]

// cat 01-DotNetPlatformAndRuntime.csproj
    // <Project Sdk="Microsoft.NET.Sdk">
    // <PropertyGroup>
    //     <OutputType>Exe</OutputType>
    //     <TargetFramework>net10.0</TargetFramework>
    //     <RootNamespace>_01_DotNetPlatformAndRuntime</RootNamespace>
    //     <ImplicitUsings>enable</ImplicitUsings>
    //     <Nullable>enable</Nullable>
    // </PropertyGroup>
    // </Project>

// dotnet restore
    // Build succeeded in 2.7s

// dotnet build
    // Build succeeded in 4.2s

// dotnet run
    // Hello from .NET!
    // Current date and time: 10/9/2026 4:17:00 PM
    // Number of values: 5

// find bin obj -type f
    // obj/Debug/net10.0/.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
    // obj/Debug/net10.0/ref/01-DotNetPlatformAndRuntime.dll
    // obj/Debug/net10.0/01-DotNetPlatformAndRuntime.csproj.FileListAbsolute.txt
    // obj/Debug/net10.0/01-DotNetPlatformAndRuntime.GeneratedMSBuildEditorConfig.editorconfig
    // obj/Debug/net10.0/01-DotNetPlatformAndRuntime.AssemblyInfo.cs
    // obj/Debug/net10.0/01-DotNetPlatformAndRuntime.genruntimeconfig.cache
    // obj/Debug/net10.0/apphost
    // obj/Debug/net10.0/01-DotNetPlatformAndRuntime.GlobalUsings.g.cs
    // obj/Debug/net10.0/01-DotNetPlatformAndRuntime.dll
    // obj/Debug/net10.0/01-DotNetPlatformAndRuntime.AssemblyInfoInputs.cache
    // obj/Debug/net10.0/01-DotNetPlatformAndRuntime.sourcelink.json
    // obj/Debug/net10.0/01-DotNetPlatformAndRuntime.assets.cache
    // obj/Debug/net10.0/01-DotNetPlatformAndRuntime.pdb
    // obj/Debug/net10.0/01-DotNetPlatformAndRuntime.csproj.CoreCompileInputs.cache
    // obj/Debug/net10.0/refint/01-DotNetPlatformAndRuntime.dll
    // obj/01-DotNetPlatformAndRuntime.csproj.nuget.g.targets
    // obj/01-DotNetPlatformAndRuntime.csproj.nuget.dgspec.json



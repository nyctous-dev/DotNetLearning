# NuGet and Dependencies

## Micro Goal

Understand how NuGet manages external libraries in .NET projects, how to add and restore package dependencies, and how to inspect and control package versions so applications build consistently.

## Concepts Covered

- NuGet and package management
- Packages, dependencies, and package sources
- The .NET CLI package commands
- The `PackageReference` element in `.csproj` files
- Direct and transitive dependencies
- Package versions and version constraints
- Package restore and `obj/project.assets.json`
- NuGet configuration and package sources
- Central Package Management
- Dependency compatibility and security considerations

## Completion Criteria

- [ ] Explain what NuGet is and why .NET applications use packages.
- [ ] Add a NuGet package to a project using the .NET CLI.
- [ ] Identify package references in a `.csproj` file.
- [ ] Restore and build a project with package dependencies.
- [ ] Distinguish direct dependencies from transitive dependencies.
- [ ] Inspect installed package dependencies and versions.
- [ ] Explain the purpose of `obj/project.assets.json`.
- [ ] Remove a package reference cleanly.
- [ ] Explain why package versions and sources matter.
- [ ] Complete the challenge with a working project and reproducible package setup.

## Lecture

### 1. What NuGet is

NuGet is the package management system used by .NET. It provides a way to distribute, install, update, and restore reusable libraries and tools.

A .NET application does not need to implement every capability itself. Instead, it can use packages that provide functionality such as JSON processing, database access, testing, logging, or HTTP integrations.

For example, .NET already includes `System.Text.Json` for JSON serialization. If a project needs a library that provides functionality not included in the platform, NuGet can supply it.

A **package** is a distributable unit, usually containing compiled assemblies, metadata, and potentially other files. A **dependency** is a library or package that a project requires to build or run.

NuGet packages are commonly retrieved from the public NuGet Gallery at `https://www.nuget.org/`, but organizations can also use private feeds or other configured package sources.

### 2. How packages relate to a project

A .NET project declares its dependencies in its `.csproj` file. The package reference identifies which package the project requires and may specify its version.

For example, a project could include:

```xml
<ItemGroup>
  <PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
</ItemGroup>
```

This declares a dependency on the `Newtonsoft.Json` package at version `13.0.4`.

The package reference is part of the project's configuration. The package's actual files are normally downloaded into the machine's NuGet package cache when dependencies are restored.

A package reference does not mean that the library's source code is copied into your project. Your project references the package, and the build system resolves the assemblies and other assets needed for compilation and execution.

Package versions in examples should always be checked against the package's available releases and compatibility requirements before use. In this module, use the version shown above unless you have a specific reason to select another compatible version.

### 3. Adding a package with the .NET CLI

The .NET CLI can add a package reference to an existing project.

From the repository root, run:

```bash
dotnet add 02-dotnet-fundamentals/03-NuGetAndDependencies/03-NuGetAndDependencies.csproj package Newtonsoft.Json --version 13.0.4
```

This command adds a package reference to the specified project. It does not add the dependency to every project in the repository.

You can also navigate into the project directory and run:

```bash
dotnet add package Newtonsoft.Json --version 13.0.4
```

When a package is added, the CLI updates the `.csproj` file. Depending on the SDK version and command behavior, it may also initiate package restoration.

Inspect the `.csproj` file afterward. You should find a package reference similar to:

```xml
<ItemGroup>
  <PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
</ItemGroup>
```

The project file is the authoritative declaration of the direct package dependency.

### 4. Using a package in C# code

After adding a package and restoring dependencies, you can use the library's namespaces and types in your code.

For example, `Newtonsoft.Json` provides JSON serialization and deserialization:

```csharp
using Newtonsoft.Json;

var simulation = new
{
    Name = "Boids",
    BoidCount = 100,
    IsRunning = true
};

string json = JsonConvert.SerializeObject(simulation);

Console.WriteLine(json);
```

The output is a JSON representation of the anonymous object.

A package is useful only if your project actually uses its functionality. Adding unnecessary dependencies increases the amount of software your project must manage and can make maintenance more complicated.

For this exercise, using `Newtonsoft.Json` is intentional: it gives the project a real package dependency that can be added, inspected, used, and removed.

### 5. Restoring dependencies

Package restore resolves the project's package references and obtains the required package assets.

Run:

```bash
dotnet restore 02-dotnet-fundamentals/03-NuGetAndDependencies/03-NuGetAndDependencies.csproj
```

Restore uses the project configuration, package sources, and dependency information to resolve packages. It downloads packages that are missing from the local cache.

You will also commonly see:

```bash
dotnet build 02-dotnet-fundamentals/03-NuGetAndDependencies/03-NuGetAndDependencies.csproj
```

`dotnet build` normally performs restore automatically when necessary. Running `dotnet restore` explicitly is useful when diagnosing dependency problems or separating restore from compilation.

A successful restore means the required dependencies could be resolved. It does not guarantee that your application has no compile-time or runtime errors.

### 6. The package cache and `project.assets.json`

NuGet normally stores downloaded packages in a global package cache. On Linux, the default location is typically:

```text
~/.nuget/packages/
```

For example, the contents of the `Newtonsoft.Json` package may be stored under a path resembling:

```text
~/.nuget/packages/newtonsoft.json/13.0.4/
```

This cache allows multiple projects to reuse the same downloaded package version.

During restore, .NET also generates files under the project's `obj/` directory. One important file is:

```text
obj/project.assets.json
```

This file records the resolved dependency graph and the assets selected for the project. The build system uses it to determine which package assets are available for compilation and other build operations.

The file is generated, not normally edited by hand. If it is missing or out of date, restoring the project generally regenerates it.

The `obj/` directory is build output and is normally excluded from version control. You should commit the project file that declares your dependencies, not the generated assets file or your local NuGet package cache.

### 7. Direct and transitive dependencies

A **direct dependency** is a package that your project references explicitly.

A **transitive dependency** is a package required by another package in your dependency graph.

Suppose your project references Package A, and Package A requires Package B. Your project may receive Package B as a transitive dependency even though your `.csproj` does not directly reference it.

Conceptually:

```text
Your project
└── Package A (direct)
    └── Package B (transitive)
```

This allows package authors to distribute libraries without requiring every consuming project to manually reference all the libraries those packages depend on.

However, transitive dependencies still matter. Their versions and compatibility can affect your build, runtime behavior, security posture, and deployment size.

Do not add direct references to every transitive package by default. Add a direct dependency when your project uses that package directly or when you have a deliberate dependency-management reason to control it.

### 8. Inspecting dependencies

The .NET CLI provides commands for examining a project's dependency graph.

To list packages and their dependencies, run:

```bash
dotnet list 02-dotnet-fundamentals/03-NuGetAndDependencies/03-NuGetAndDependencies.csproj package --include-transitive
```

The output identifies direct and transitive packages and their resolved versions. Exact output formatting varies by SDK version.

To inspect outdated package references, run:

```bash
dotnet list 02-dotnet-fundamentals/03-NuGetAndDependencies/03-NuGetAndDependencies.csproj package --outdated
```

This command can help identify available newer versions, but an update is not automatically advisable. Newer versions may introduce breaking changes or require a different target framework.

When reviewing dependencies, consider:

- Whether the project actually uses the package.
- Whether the version supports the project's target framework.
- Whether newer releases contain important fixes.
- Whether the package introduces additional dependencies.
- Whether the package source is trusted.

### 9. Package versions and compatibility

A package version is part of the dependency declaration. Pinning a specific version makes the intended direct dependency explicit:

```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
```

A different version may contain bug fixes, performance improvements, security fixes, or breaking changes.

Updating packages should be deliberate. After an update, restore and build the project, run tests, and verify behavior. For a larger application, review release notes and compatibility requirements before upgrading.

NuGet also supports version ranges and floating versions in certain configurations. These can be useful in specialized dependency-management workflows, but they may make resolution less predictable if used without a clear policy.

For a beginner learning repository, explicit versions are easier to reason about. They make the intended direct dependency clear when someone reviews the project file.

A project file alone does not necessarily guarantee that every transitive dependency will resolve identically forever. More advanced workflows can use a lock file to record the resolved dependency graph and enable locked restores. Learn the basic package-reference workflow first before introducing that additional mechanism.

### 10. Package sources and NuGet configuration

NuGet obtains packages from configured package sources. The public NuGet Gallery is the common default source, but projects may also use private feeds.

NuGet configuration can be provided at different levels, including machine-wide, user-level, and repository or directory-level configuration.

A `NuGet.Config` file can specify package sources and other restore settings. For example:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

This example clears inherited package sources and explicitly adds the public NuGet Gallery feed. Because `<clear />` removes inherited sources for this configuration section, use it only when that behavior is intended.

Do not add a configuration file merely for the sake of having one. Default NuGet configuration is sufficient for many personal projects.

Be cautious with package sources. Installing a package means trusting its code and distribution process. Avoid unknown feeds, review package identity and maintenance history, and do not commit authentication credentials or API keys to a repository.

### 11. Central Package Management

When a solution contains multiple projects, each project can declare its own package versions. Over time, this can lead to inconsistent versions across projects.

NuGet supports **Central Package Management**, which lets a repository define package versions in a `Directory.Packages.props` file.

A simplified example is:

```xml
<Project>
  <ItemGroup>
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.4" />
  </ItemGroup>
</Project>
```

With Central Package Management enabled and configured appropriately, individual project files can reference the package without specifying its version:

```xml
<ItemGroup>
  <PackageReference Include="Newtonsoft.Json" />
</ItemGroup>
```

This feature is useful when several projects intentionally share package dependencies. It is not required for a single small console project, and introducing it adds another configuration concept to understand.

For this module, keep the package reference directly in the project file. Central Package Management is a useful pattern to recognize for future multi-project applications.

### 12. Removing and updating packages

If a project no longer needs a package, remove the reference rather than leaving an unused dependency behind.

From the repository root, run:

```bash
dotnet remove 02-dotnet-fundamentals/03-NuGetAndDependencies/03-NuGetAndDependencies.csproj package Newtonsoft.Json
```

Removing the reference updates the project file. The package may remain in the local cache because other projects may use it, or because NuGet does not automatically remove every cached package. That is normal.

To update a dependency, inspect the available versions and deliberately change the package reference. Then restore, build, and test the project.

For example, you can add the package again at the desired version using `dotnet add package`, or edit the `.csproj` version and run restore.

Avoid manually deleting generated files as a substitute for managing package references. The project file expresses the dependency; restore regenerates the supporting dependency assets.

### 13. Dependency management practices

Good dependency management keeps projects understandable and maintainable.

- Add only packages that the project needs.
- Prefer established packages with clear documentation and a credible maintenance history.
- Use explicit package versions for predictable direct dependencies.
- Inspect transitive dependencies when diagnosing conflicts or security issues.
- Restore and build after changing package references.
- Run tests after upgrading packages.
- Keep generated build and restore artifacts out of version control.
- Keep secrets out of source code and configuration committed to Git.
- Use a package lock file or central package management when the size and needs of the solution justify them.

The key distinction is that the `.csproj` declares what the project needs, NuGet resolves those dependencies, and the build system consumes the resulting assets. Understanding that workflow is more important than memorizing every available NuGet command.

## Challenge

Modify the `03-NuGetAndDependencies` console project to demonstrate a complete NuGet dependency workflow.

Your program must satisfy all of the following requirements:

1. **Add a package:** Add `Newtonsoft.Json` version `13.0.4` to the project using the .NET CLI.
2. **Inspect the project:** Confirm that the package reference appears in `03-NuGetAndDependencies.csproj`.
3. **Use the package:** Define an object containing a simulation name, boid count, and running state. Serialize it to JSON using `JsonConvert.SerializeObject`.
4. **Display the result:** Print the serialized JSON to the console.
5. **Restore dependencies:** Run `dotnet restore` successfully for the project.
6. **Inspect dependencies:** Run the package-list command with transitive dependencies included and identify the direct `Newtonsoft.Json` dependency.
7. **Build and run:** Build the project and run it successfully.
8. **Understand generated assets:** Locate `obj/project.assets.json` and explain why it is generated rather than maintained manually.
9. **Understand removal:** Determine the CLI command that would remove `Newtonsoft.Json`, but do not remove the package before completing the working demonstration.
10. **Keep the project focused:** Use the package for actual functionality, keep the code concise, and do not add unrelated dependencies.

Completion means the project builds, runs, and produces valid JSON, and you can explain the relationship between the project file, NuGet restore, the package cache, and the generated dependency assets.
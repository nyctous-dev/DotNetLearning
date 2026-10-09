# Build and Execution

## Micro Goal

Understand how .NET restores dependencies, compiles a project, runs an application, and publishes it for deployment. Learn the difference between development-time execution and a deployable application, and become comfortable using the .NET CLI to control each stage.

## Concepts Covered

- The .NET application lifecycle
- `dotnet restore`
- `dotnet build`
- `dotnet run`
- `dotnet clean`
- `dotnet publish`
- Build configurations: `Debug` and `Release`
- Build output and intermediate files
- Target frameworks and runtime identifiers
- Framework-dependent deployment
- Self-contained deployment
- Application entry points and process exit codes
- Build errors versus runtime errors
- Reproducible builds and dependency restoration

## Completion Criteria

- [ ] Explain the difference between restoring, building, running, cleaning, and publishing a .NET application.
- [ ] Use the .NET CLI to restore, build, and run the project.
- [ ] Explain the roles of the `bin/` and `obj/` directories.
- [ ] Build the application in both `Debug` and `Release` configurations.
- [ ] Locate and inspect the compiled output.
- [ ] Publish a framework-dependent application and run the published output.
- [ ] Explain the difference between framework-dependent and self-contained deployments.
- [ ] Identify whether a failure occurs during compilation or at runtime.
- [ ] Complete the challenge using the existing numbered console project.

## Lecture

### 1. The .NET application lifecycle

A C# source file is not normally executed directly by the .NET runtime. It is part of a project that describes how the application should be built.

A typical .NET application lifecycle contains several stages:

1. **Restore:** Resolve and obtain the dependencies required by the project.
2. **Build:** Compile the source code and produce application output.
3. **Run:** Build the project if necessary, then launch the application.
4. **Publish:** Prepare the application and its required deployment files for distribution.

These stages are related, but they are not interchangeable. A successful restore does not prove that the code compiles. A successful build does not prove that the program behaves correctly at runtime. A successful run on your development machine does not automatically mean the application is ready to deploy elsewhere.

The .NET CLI provides commands for each stage. You can run these commands from the directory containing the project file or specify a project path explicitly.

For this module, use the existing console project:

```bash
dotnet run --project 02-dotnet-fundamentals/04-BuildAndExecution/04-BuildAndExecution.csproj
```

When running commands from inside the project directory, you can use shorter forms such as `dotnet run` or `dotnet build`.

### 2. Restoring dependencies

The `dotnet restore` command resolves the dependencies declared by the project and its referenced projects. It obtains the necessary NuGet packages and records the resolved dependency information used by the build.

```bash
dotnet restore 02-dotnet-fundamentals/04-BuildAndExecution/04-BuildAndExecution.csproj
```

A project can reference packages explicitly in its `.csproj` file:

```xml
<PackageReference Include="Example.Package" Version="1.0.0" />
```

This is an illustrative package reference, not a package you should add to this project.

Even a simple console application has a project definition and framework dependencies. It may not need any additional NuGet packages, but restore is still part of the normal build workflow.

The .NET CLI automatically restores dependencies when required by commands such as `dotnet build`, `dotnet run`, and `dotnet publish`. Running restore separately is useful when you want to confirm dependencies are available or troubleshoot dependency-resolution problems.

A successful restore means the dependencies were resolved. It does not mean the C# source code is valid.

### 3. Building an application

The `dotnet build` command compiles a project and its dependencies as needed:

```bash
dotnet build 02-dotnet-fundamentals/04-BuildAndExecution/04-BuildAndExecution.csproj
```

If the project compiles successfully, the command reports a successful build and produces output under `bin/` by default.

Builds can fail for several reasons, including syntax errors, type mismatches, missing references, and incompatible project settings. The compiler reports errors with locations and diagnostic codes.

For example, this statement is invalid because a string cannot be implicitly assigned to an integer:

```csharp
int boidCount = "one hundred";
```

The project cannot compile until the error is corrected. This is a **build-time error**, not a runtime error: the invalid program never reaches normal execution.

A successful build confirms that the project compiled under the selected build settings. It does not prove that the application has correct logic or that every possible runtime path works.

### 4. Running an application

The `dotnet run` command runs a .NET project. By default, it builds the project if necessary before launching it.

```bash
dotnet run --project 02-dotnet-fundamentals/04-BuildAndExecution/04-BuildAndExecution.csproj
```

For example, a console application's `Program.cs` might contain:

```csharp
Console.WriteLine("Application started.");

int boidCount = 100;
Console.WriteLine($"Boid count: {boidCount}");
```

Running the application displays:

```text
Application started.
Boid count: 100
```

Use `dotnet run` during development because it combines the build-and-launch workflow. Use `dotnet build` when you want to compile without launching the program.

The `--no-build` option tells `dotnet run` to skip building:

```bash
dotnet run --no-build --project 02-dotnet-fundamentals/04-BuildAndExecution/04-BuildAndExecution.csproj
```

Only use `--no-build` when suitable compiled output already exists. Otherwise, the command may fail or run stale output.

### 5. Understanding build output

.NET projects normally produce two important directories:

- `obj/` contains intermediate build files, generated files, and information used during compilation.
- `bin/` contains the final build output for a particular configuration and target framework.

For a typical project targeting .NET 10, a build might produce a structure similar to:

```text
04-BuildAndExecution/
├── 04-BuildAndExecution.csproj
├── Program.cs
├── obj/
└── bin/
    ├── Debug/
    │   └── net10.0/
    └── Release/
        └── net10.0/
```

The exact contents vary with the SDK, project settings, and build configuration. The `obj/` directory can contain generated source code, dependency information, and intermediate artifacts. The `bin/` directory can contain the compiled application assembly, runtime configuration, dependency metadata, and other output files.

You generally should not edit generated files in either directory. They are build artifacts and can be regenerated.

These directories are normally excluded from Git using the standard .NET `.gitignore` rules. Commit your source code, project files, and relevant documentation rather than generated output.

### 6. Debug and Release configurations

.NET projects commonly use two build configurations:

- `Debug` is intended for development and debugging.
- `Release` is intended for optimized builds and deployment preparation.

Build the project in Debug mode:

```bash
dotnet build 02-dotnet-fundamentals/04-BuildAndExecution/04-BuildAndExecution.csproj --configuration Debug
```

Build it in Release mode:

```bash
dotnet build 02-dotnet-fundamentals/04-BuildAndExecution/04-BuildAndExecution.csproj --configuration Release
```

The `--configuration` option is also commonly written as `-c`.

The output normally goes into separate directories:

```text
bin/Debug/net10.0/
bin/Release/net10.0/
```

Debug builds commonly retain settings useful for debugging. Release builds typically use optimized compilation settings. The exact behavior depends on the project and its build configuration.

Release does not mean the program has been tested, and Debug does not mean the program cannot be run outside the development environment. These configurations control how the project is built; they do not replace testing.

### 7. Cleaning build output

The `dotnet clean` command removes outputs from a previous build:

```bash
dotnet clean 02-dotnet-fundamentals/04-BuildAndExecution/04-BuildAndExecution.csproj
```

You can specify a configuration:

```bash
dotnet clean 02-dotnet-fundamentals/04-BuildAndExecution/04-BuildAndExecution.csproj --configuration Release
```

Cleaning can help when you need to remove old build output before rebuilding. It is not a substitute for fixing source errors, dependency problems, or incorrect project settings.

A typical clean rebuild sequence is:

```bash
dotnet clean
dotnet restore
dotnet build
```

Run these commands from the project directory. In most normal workflows, the CLI handles restore automatically, so a separate restore is not always necessary.

### 8. Publishing an application

Building produces output for development and compilation. Publishing prepares output for deployment.

Use `dotnet publish` to produce a publish directory:

```bash
dotnet publish 02-dotnet-fundamentals/04-BuildAndExecution/04-BuildAndExecution.csproj --configuration Release
```

By default, publishing writes output under a path similar to:

```text
bin/Release/net10.0/publish/
```

The publish directory may contain the application assembly, `.deps.json` dependency metadata, `.runtimeconfig.json` runtime settings, and other files required to run the application.

Publish output is distinct from the normal build output. When deploying an application, use the published output appropriate for the deployment target rather than assuming that copying the entire project directory is sufficient.

Publishing a console application does not automatically turn it into a web application, create an installer, or deploy it to a server. Publishing prepares the application files; deployment is the separate process of placing and running those files in the target environment.

### 9. Framework-dependent and self-contained deployment

A .NET application can be published in different deployment modes.

**Framework-dependent deployment** expects a compatible .NET runtime to be installed on the target machine. It generally produces a smaller deployment because it does not include the runtime itself.

```bash
dotnet publish 02-dotnet-fundamentals/04-BuildAndExecution/04-BuildAndExecution.csproj --configuration Release --self-contained false
```

The target machine must have a compatible runtime installed. For a framework-dependent application, the `dotnet` command can launch the application assembly:

```bash
dotnet 04-BuildAndExecution.dll
```

Run that command from the publish directory, where the DLL exists.

**Self-contained deployment** includes the .NET runtime needed by the application. It can run without a separately installed .NET runtime, but the deployment is larger and is specific to a target platform and architecture.

For example, publish for Linux x64:

```bash
dotnet publish 02-dotnet-fundamentals/04-BuildAndExecution/04-BuildAndExecution.csproj --configuration Release --self-contained true --runtime linux-x64
```

A self-contained deployment for Linux x64 is not interchangeable with one for Windows or macOS. Choose the runtime identifier appropriate for the deployment target.

A self-contained application can also be published as a single file when the project and deployment options support it. Single-file publishing is a separate choice from whether the deployment is self-contained.

For this learning project, framework-dependent publishing is sufficient to understand the workflow. You do not need to produce a self-contained deployment unless you want to compare the results.

### 10. Runtime errors and exit codes

A program can compile successfully and still fail during execution. Such failures are runtime errors.

For example:

```csharp
int divisor = 0;
int result = 10 / divisor;

Console.WriteLine(result);
```

This code is valid C# syntax and can compile, but executing it causes a `DivideByZeroException`.

This illustrates the distinction:

- **Build-time failure:** The application cannot be compiled successfully.
- **Runtime failure:** The application starts executing but encounters an error during execution.

Console applications also finish with an exit code. By convention, exit code `0` indicates success, while a nonzero code commonly indicates failure.

You can set the process exit code explicitly:

```csharp
Environment.ExitCode = 0;
```

Avoid using `Environment.Exit` for ordinary control flow because it terminates the process immediately. In most applications, allow the program to finish naturally and set a nonzero exit code when an operation fails in a way that should be reported to the calling process.

### 11. A practical development workflow

For routine development, the following sequence is usually sufficient:

```bash
dotnet restore
dotnet build
dotnet run
```

When preparing a release, use:

```bash
dotnet build --configuration Release
dotnet publish --configuration Release
```

The explicit restore command is useful when diagnosing package problems, but it is not mandatory before every build or run.

The key is to know what each command guarantees:

- Restore resolves dependencies.
- Build compiles the project.
- Run launches the application, building first if needed.
- Clean removes previous build outputs.
- Publish prepares the application for deployment.

These distinctions become especially important as projects grow to include multiple assemblies, third-party packages, tests, and deployment targets. Understanding the build pipeline makes it easier to isolate failures and produce predictable application output.

## Challenge

Use this console project to demonstrate the .NET build and execution lifecycle. Keep the C# program simple; the focus is on running CLI commands and understanding the artifacts they produce.

1. **Inspect the project:** Identify the `.csproj` file and `Program.cs`. Explain what each file contributes to the build.
2. **Restore dependencies:** Run `dotnet restore` and confirm that it completes successfully.
3. **Build the project:** Run `dotnet build` and confirm that compilation succeeds.
4. **Run the project:** Update `Program.cs` to print a startup message and a simple value, such as a boid count. Run the application and verify the output.
5. **Inspect build output:** Find the `bin/` and `obj/` directories. Identify the target framework directory and describe the purpose of each output location.
6. **Compare configurations:** Build the project in both Debug and Release configurations. Locate both sets of outputs.
7. **Clean and rebuild:** Run `dotnet clean`, rebuild the project, and confirm that it runs correctly afterward.
8. **Publish the application:** Publish a Release, framework-dependent build. Locate the publish directory and identify the main application assembly and its runtime configuration file.
9. **Run the published output:** Execute the published application from its publish directory using `dotnet` and the published DLL name. Confirm that its output matches the normal run.
10. **Explain the lifecycle:** In your own words, describe the differences between restore, build, run, clean, and publish. Also explain why a successful build does not guarantee a successful runtime execution.

Keep the source code focused on demonstrating execution. Use the terminal commands to investigate the lifecycle rather than adding unrelated application logic.
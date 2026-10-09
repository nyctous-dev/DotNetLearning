# .NET Platform and Runtime

## Micro Goal

Understand what .NET provides, how the SDK and runtime differ, and how C# source code is compiled and executed by the Common Language Runtime (CLR).

## Concepts Covered

- The .NET platform
- C# versus .NET
- .NET SDK versus .NET runtime
- Common Language Runtime (CLR)
- Intermediate Language (IL)
- Just-in-time (JIT) compilation
- Managed code and garbage collection
- Base Class Library (BCL)
- Target frameworks and framework-dependent applications
- .NET CLI commands
- Inspecting installed SDKs and runtimes
- Building and running a console application

## Completion Criteria

- [ ] Explain the difference between C# and .NET.
- [ ] Explain the roles of the .NET SDK and runtime.
- [ ] Describe how C# source code becomes executable instructions.
- [ ] Explain the roles of the CLR, IL, and JIT compiler.
- [ ] Distinguish managed code from native code at a basic level.
- [ ] Explain what the garbage collector does.
- [ ] Identify the purpose of the Base Class Library.
- [ ] Inspect installed .NET SDKs and runtimes using the CLI.
- [ ] Build and run a console application using explicit CLI commands.
- [ ] Explain how a target framework relates to the installed .NET environment.

## Lecture

### 1. What is .NET?

.NET is a development platform for building and running applications. It provides a runtime, standard libraries, development tools, and infrastructure that work together to support application development.

You can use .NET to build console applications, web applications, APIs, desktop applications, background services, and other types of software.

For this learning project, the main focus is the .NET platform that runs C# applications and will eventually support the Blazor-based Boids simulation.

C# and .NET are related, but they are not the same thing.

- **C#** is a programming language. It defines syntax, types, expressions, classes, methods, and other language features.
- **.NET** is the platform that provides the runtime, libraries, and tools used to build and execute applications written in C# and other supported languages.

For example:

```csharp
Console.WriteLine("Hello from .NET");
```

This is C# source code. The `Console` type is provided by the .NET class libraries, and the .NET runtime provides the execution environment for the compiled application.

A useful way to think about the relationship is that C# describes the program, while .NET provides much of the infrastructure required to build and run it.

### 2. The .NET SDK and runtime

The .NET installation includes components with different responsibilities.

The **.NET SDK** is the development toolkit. It includes the tools needed to create projects, compile source code, restore dependencies, and publish applications. It also includes a compatible runtime.

The **.NET runtime** provides the environment needed to execute managed .NET applications. It includes the CLR and the runtime libraries required by applications that target that runtime.

You generally need the SDK when developing applications. A machine that only needs to run a compatible framework-dependent application may only need the appropriate runtime, although deployment options can bundle the runtime with the application.

Check the installed SDKs:

```bash
dotnet --list-sdks
```

Check the installed runtimes:

```bash
dotnet --list-runtimes
```

Display information about the current .NET installation:

```bash
dotnet --info
```

These commands answer different questions:

- `dotnet --list-sdks` shows which SDK versions are installed.
- `dotnet --list-runtimes` shows which runtime versions are installed.
- `dotnet --info` provides broader information about the installation, including the operating system, architecture, SDKs, and runtimes.

Having an SDK installed does not mean every possible target framework or runtime is installed. The SDK, target framework, and runtime versions are related but distinct concepts.

### 3. How a C# application is compiled and executed

A C# application generally passes through several stages between source code and execution.

First, you write source code in `.cs` files. The C# compiler processes those files and produces an assembly containing Intermediate Language (IL) and metadata.

For a typical modern .NET application, the assembly is a `.dll` file. The application may also have a native executable launcher, depending on how it was built.

At a high level, the process looks like this:

<CodeBlock language="mermaid">
flowchart TD
    A["C# source files (.cs)"] 
    A --> B["C# compiler"]
    B --> C["Assembly: IL and metadata"]
    C --> D[".NET runtime and CLR"]
    D --> E["JIT compiler"]
    E --> F["Native machine instructions"]
    F --> G["Program executes"]
</CodeBlock>

The stages have different responsibilities:

1. **C# source code:** Contains the instructions and declarations you write.
2. **C# compiler:** Checks the code for errors and translates it into IL and metadata.
3. **Assembly:** Contains compiled code and information about the types and members in the program.
4. **CLR:** Provides the managed execution environment, including services such as exception handling and garbage collection.
5. **JIT compiler:** Compiles IL into native machine instructions as needed during execution.
6. **Execution:** The processor executes the native instructions.

This is a simplified view. The runtime may perform additional work, and some applications can use ahead-of-time compilation instead of relying entirely on JIT compilation. For the console applications in this learning section, the usual compile-to-IL and runtime execution model is sufficient.

### 4. Intermediate Language and assemblies

C# code is not normally compiled directly into a platform-specific native executable during the ordinary build process. Instead, the compiler emits **Intermediate Language**, commonly abbreviated IL.

IL is a lower-level instruction representation designed for the .NET execution environment. The resulting assembly also contains metadata describing types, methods, references, and other information needed by the runtime.

For example, a project might produce:

```text
bin/
└── Debug/
    └── net10.0/
        ├── MyApplication.dll
        ├── MyApplication.deps.json
        └── MyApplication.runtimeconfig.json
```

The exact files depend on the project and build settings.

- The `.dll` contains the managed assembly.
- The `.deps.json` file records dependency information used by the application host.
- The `.runtimeconfig.json` file records runtime configuration, including the framework information relevant to launching the application.

The output directory also contains other files as needed. You normally do not edit these generated files directly.

IL and metadata allow .NET to provide a consistent programming model across supported platforms. However, the target framework, runtime availability, native dependencies, and deployment configuration still matter when moving an application between machines.

### 5. The Common Language Runtime (CLR)

The **Common Language Runtime**, or CLR, is the execution engine for managed .NET code.

The CLR provides services that let applications execute without requiring every application to implement those services itself.

Important responsibilities include:

- Loading assemblies and resolving type information.
- Executing managed code.
- Coordinating JIT compilation.
- Managing memory through garbage collection.
- Handling managed exceptions.
- Enforcing runtime type safety and other execution rules.

The CLR is not the C# language itself. It is part of the platform that executes managed code produced by the C# compiler and compilers for other .NET languages.

This separation is useful because different .NET languages can use the same runtime and libraries. A C# application and a Visual Basic .NET application can both target .NET, even though their source languages differ.

### 6. Just-in-time compilation

The **Just-in-Time compiler**, or JIT compiler, translates IL into native machine instructions that the processor can execute.

In the typical JIT execution model, the C# compiler emits IL during the build. At runtime, the JIT compiler converts the relevant methods into native instructions as they are needed.

This allows the same managed assembly to be used on different supported processor architectures when the appropriate runtime is available. The JIT compiler generates native instructions appropriate for the environment in which the application runs.

For example, an assembly built for a supported .NET target can potentially run on both Linux and Windows, provided the application and its dependencies are compatible with each environment.

JIT compilation does not eliminate platform considerations. Applications can still depend on operating-system APIs, native libraries, architecture-specific code, or platform-specific configuration.

.NET also supports ahead-of-time compilation options. These can compile code to native machine code before deployment, changing aspects of startup and deployment. You do not need to use those options for the projects in this module.

### 7. Managed code and garbage collection

Code executed under the .NET runtime's managed execution model is commonly called **managed code**.

The runtime provides services that simplify application development, particularly memory management.

When you create a reference-type object:

```csharp
Boid boid = new Boid();
```

the object is normally allocated on the managed heap. The variable `boid` holds a reference to that object.

When an object is no longer reachable from the application's live references and runtime roots, it may become eligible for garbage collection. The garbage collector can reclaim memory occupied by objects that are no longer needed.

This means you generally do not manually free each managed object as you would with explicit memory allocation and deallocation in some lower-level languages.

However, garbage collection does not mean memory is reclaimed immediately when a variable goes out of scope. The runtime determines when to perform collection, and objects can remain alive as long as references keep them reachable.

Garbage collection also does not automatically release every external resource at the right time. File handles, streams, and similar resources often require explicit cleanup through `IDisposable` and `using`. Resource management will be covered in a later module.

### 8. The Base Class Library (BCL)

The **Base Class Library**, or BCL, provides reusable types and APIs for common programming tasks.

Instead of implementing everything from scratch, you can use library types for strings, collections, dates, files, networking, and many other operations.

Examples include:

```csharp
string message = "Hello";
int count = 3;

List<int> values = new() { 10, 20, 30 };

DateTime now = DateTime.Now;

Console.WriteLine(message);
Console.WriteLine(values.Count);
Console.WriteLine(now);
```

These examples use common types provided by .NET libraries.

The BCL is a fundamental part of the platform. ASP.NET Core and other .NET technologies build on top of the runtime and libraries to provide additional capabilities.

The distinction is important: `List<T>` is a library type, while the syntax for declaring a variable or calling a method comes from the C# language.

### 9. Target frameworks

A .NET project specifies the framework it targets. This is commonly configured in the project file using the `TargetFramework` property.

For example:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

The `net10.0` target indicates that the project targets .NET 10. The other properties configure the project:

- `OutputType` set to `Exe` indicates an executable application.
- `ImplicitUsings` enables common global namespace imports generated by the SDK.
- `Nullable` enables nullable reference type analysis.

A target framework is not the same thing as the installed SDK version. The SDK provides build tools; the target framework determines which framework APIs and reference assemblies the project compiles against; the runtime executes the application.

For a framework-dependent application, the required runtime must be available on the machine unless it is provided through another deployment arrangement.

The SDK can also support targeting frameworks other than its own major version, subject to the SDK and reference packs available. Do not assume that the newest installed SDK automatically means every target framework is installed or supported in every environment.

### 10. Build, run, and inspect a project

The .NET CLI provides commands for common development tasks.

From a project directory, run:

```bash
dotnet restore
dotnet build
dotnet run
```

Each command has a distinct purpose:

- `dotnet restore` resolves and downloads the project's NuGet dependencies.
- `dotnet build` compiles the project and reports compilation errors.
- `dotnet run` builds the project if needed and launches it.

You can also run commands from another directory by specifying the project file:

```bash
dotnet build 01-DotNetPlatformAndRuntime.csproj
dotnet run --project 01-DotNetPlatformAndRuntime.csproj
```

To inspect the build output, look inside the `bin` directory. Intermediate build files are stored under `obj`. These directories are generated by the build process and normally should not be committed to Git.

For a more controlled sequence, you can explicitly build first and then run the resulting application. This helps distinguish a compilation failure from a runtime failure.

A successful build proves that the compiler accepted the code and produced build output. It does not prove that every execution path is correct or that the application behaves as intended.

### 11. Development and deployment

During development, `dotnet run` is convenient because it builds and launches the project with minimal manual setup.

Deployment is a separate concern. A published application can be prepared using:

```bash
dotnet publish -c Release
```

Publishing creates output intended for deployment. The precise output depends on the project, target framework, runtime identifier, and publish settings.

A framework-dependent deployment expects a compatible .NET runtime to be installed on the target machine. A self-contained deployment can include the runtime components needed by the application, increasing the deployment size but reducing reliance on a separately installed runtime.

For example, a Linux deployment may target a particular Linux runtime identifier and processor architecture. A deployment intended for a different operating system or architecture may need to be published separately.

For this module, understand the distinction between building and publishing. You will use the normal development workflow for the learning projects and explore deployment choices when they become relevant.

## Challenge

Create a small console application that demonstrates your understanding of the .NET platform and runtime.

Your program and accompanying investigation must satisfy all of the following requirements:

1. **Inspect the environment:** Run `dotnet --info`, `dotnet --list-sdks`, and `dotnet --list-runtimes`. Record which SDK and runtime versions are installed.
2. **Inspect the project:** Open `01-DotNetPlatformAndRuntime.csproj` and identify the target framework and the purpose of the `OutputType`, `ImplicitUsings`, and `Nullable` properties.
3. **Write a simple application:** Print a message, the current date and time, and the number of values in a small `List<int>`.
4. **Use the BCL:** Use at least three standard .NET library types and explain what each one contributes.
5. **Build explicitly:** Run `dotnet restore` and `dotnet build`. Confirm that the build succeeds.
6. **Run the application:** Execute it with `dotnet run` and verify that the output matches your expectations.
7. **Inspect generated output:** Locate the project's `bin` and `obj` directories and identify the compiled assembly and runtime configuration file when present.
8. **Explain execution:** In your own words, describe how the C# source code is compiled, how IL and metadata are stored in an assembly, and how the runtime executes managed code.
9. **Distinguish platform components:** Explain the difference between the SDK, runtime, CLR, JIT compiler, and BCL.
10. **Explain deployment:** Describe the difference between building an application and publishing it, and explain what framework-dependent means.

Keep the code small. The goal is not to build a large application, but to connect the tools you use to the components that make a .NET application work.
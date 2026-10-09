# Hello World

## Micro Goal

Understand the basic structure of a C# console project, how program execution begins, and how to create, build, and run projects using the .NET CLI.

## Concepts Covered

- **Console applications** — programs that interact through terminal input and output.
- **Project structure** — the roles of `.csproj` files and `Program.cs`.
- **Entry points** — how C# begins executing a program.
- **Top-level statements** — writing executable code without an explicit `Main` method.
- **.NET CLI** — creating, building, and running projects.

## Completion Criteria

Be able to explain the basic structure of a console project, describe how execution begins, and use the .NET CLI to create, build, and run a C# application.

## Lecture

### 1. Console Applications

A console application is a program that interacts with users through text input and output in a terminal. Console applications are useful for learning programming fundamentals without the additional complexity of a graphical interface or web application.

For example:

```csharp
Console.WriteLine("Hello, World!");
```

`Console.WriteLine` prints text to the terminal and moves to a new line.

### 2. Project Structure

A basic .NET console project contains:

- **`.csproj`** — the project file, which defines configuration, target framework, and dependencies.
- **`Program.cs`** — the C# source file containing the program's executable code.
- **`bin/` and `obj/`** — generated build output and intermediate files, created when building the project.

The project directory groups the files needed to build and run that application.

### 3. Entry Points

Every executable C# application needs an entry point: the place where execution begins.

In modern C#, top-level statements allow you to write executable code directly in `Program.cs`:

```csharp
Console.WriteLine("Hello, World!");
```

The compiler generates the underlying entry point automatically. You do not need to declare a class or a `Main` method explicitly.

An equivalent traditional entry point looks like this:

```csharp
using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}
```

Both versions print the same output. Top-level statements reduce boilerplate and are convenient for small applications and learning exercises.

### 4. Creating and Running Projects

Create a new console project with:

```bash
dotnet new console -n HelloWorld
```

This creates a directory named `HelloWorld` containing the project file and source code.

Move into the project directory and run it:

```bash
cd HelloWorld
dotnet run
```

The expected output is:

```text
Hello, World!
```

Useful .NET CLI commands:

- `dotnet new console -n Name` — creates a console project.
- `dotnet run` — builds and runs the project.
- `dotnet build` — builds the project without running it.
- `dotnet clean` — removes previous build outputs.
- `dotnet --list-sdks` — lists installed .NET SDKs.

You can run a project from another directory by specifying its project file:

```bash
dotnet run --project 01-HelloWorld/01-HelloWorld.csproj
```

### 5. Projects and Repositories

A Git repository can contain many independent .NET projects. Each project has its own `.csproj` file and can have its own entry point.

Projects do not automatically share their code just because they exist in the same repository. Code reuse requires an appropriate project reference or shared library.

This repository uses separate console projects to isolate learning topics, making each exercise easier to understand, run, and test independently.

## Challenge

Create a new console project named `HelloWorld` using the .NET CLI.

1. Locate and inspect `Program.cs` and the `.csproj` file.
2. Run the application and verify that it prints `Hello, World!`.
3. Build the application using `dotnet build`.
4. Explain the role of the project file and source file.
5. Explain how top-level statements provide an entry point without an explicit `Main` method.
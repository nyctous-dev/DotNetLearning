# Hello World

## Purpose

This project is the first step in learning C# and .NET. It demonstrates the basic structure of a console application and how to create and run a .NET project.

## What Is a Console Application?

A console application is a program that interacts with the user through text input and output in a terminal. It is useful for learning programming fundamentals without the additional complexity of a graphical interface or web application.

## Program Entry Point

`Program.cs` contains the program's executable code. In this project, it uses a top-level statement:

```csharp
Console.WriteLine("Hello, World!");
```

When the application runs, execution begins with these top-level statements. C# generates the underlying entry point automatically, so an explicit `Main` method is not required.

## Project Scope

Each console application in this repository is an independent .NET project with its own `.csproj` file, source code, and entry point.

A project can access its own code and the libraries it references. It cannot automatically access the code in another project just because both projects are in the same repository. Projects must be connected through explicit project references or shared libraries when code needs to be reused.

## Creating and Running Projects

Run these commands from the directory where you want the project created:

```bash
dotnet new console -n HelloWorld
cd HelloWorld
dotnet run
```

Useful .NET CLI commands:

- `dotnet new console -n Name` — creates a console project.
- `dotnet run` — builds and runs the project.
- `dotnet build` — compiles the project without running it.
- `dotnet clean` — removes previous build outputs.

## Learning Outcome

Understand the basic structure of a C# console project, the role of `Program.cs` and the `.csproj` file, and how to create, build, and run applications using the .NET CLI.
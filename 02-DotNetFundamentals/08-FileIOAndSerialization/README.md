# File I/O and Serialization

## Micro Goal

Learn how to read and write files in C#, manage file paths, and serialize and deserialize JSON so an application can persist data and restore it later.

## Concepts Covered

- File input and output (I/O)
- `File` and `Directory` APIs
- Absolute and relative file paths
- Reading and writing text files
- `File.ReadAllText`, `File.WriteAllText`, and line-based operations
- Asynchronous file I/O
- `using` statements and resource disposal
- JSON serialization and deserialization with `System.Text.Json`
- `JsonSerializerOptions` and formatted JSON
- Handling missing files and I/O exceptions
- Nullable results and invalid JSON
- Saving and loading application state
- Basic file organization and safe data handling

## Completion Criteria

- [ ] Create a directory and write text to a file.
- [ ] Read an entire text file and read it line by line.
- [ ] Explain the difference between an absolute path and a relative path.
- [ ] Use `Path.Combine` to construct paths safely.
- [ ] Serialize a C# object to JSON and save it to a file.
- [ ] Read JSON from a file and deserialize it into a C# object.
- [ ] Handle missing files and malformed JSON without crashing unexpectedly.
- [ ] Use asynchronous file operations with `async` and `await`.
- [ ] Explain when a stream needs to be disposed and how `using` manages it.
- [ ] Complete the challenge with clear separation between data, file operations, and program flow.

## Lecture

### 1. What is file I/O?

File I/O means reading data from files and writing data to files. Variables and objects normally exist in memory while an application is running. When the application exits, that in-memory state is lost unless it is saved somewhere persistent.

For example, a Boids simulation may need to save its configuration, such as the number of boids, their movement speed, or the dimensions of the simulation area. A later run can load those values rather than requiring the user to enter them again.

C# provides several APIs for file operations. The `System.IO` namespace contains types such as `File`, `Directory`, `Path`, `StreamReader`, and `StreamWriter`.

For simple text files, the `File` class is often sufficient:

```csharp
using System.IO;

string content = "Boids simulation configuration";
File.WriteAllText("config.txt", content);

string loadedContent = File.ReadAllText("config.txt");
Console.WriteLine(loadedContent);
```

`File.WriteAllText` creates the file if it does not exist and overwrites it if it does. `File.ReadAllText` reads the complete file into a string. These methods are convenient for small files, but reading an entire large file into memory may be inefficient.

### 2. File paths and directories

A file path identifies where a file is located. Paths can be absolute or relative.

An absolute path identifies a location from the filesystem root, such as:

```text
/home/user/data/config.json
```

A relative path identifies a location relative to the application's current working directory or another explicitly chosen base directory, depending on how the path is resolved.

For example:

```csharp
string relativePath = "data/config.json";
```

This path does not guarantee that the file is located next to `Program.cs`. When running a .NET application, the current working directory may differ depending on how the application was launched.

Check the current working directory with:

```csharp
Console.WriteLine(Directory.GetCurrentDirectory());
```

Use `Path.Combine` to construct paths instead of manually concatenating strings with slash characters:

```csharp
string directory = Path.Combine(
    Directory.GetCurrentDirectory(),
    "data");

string filePath = Path.Combine(directory, "config.json");
```

`Path.Combine` uses the appropriate path separator for the operating system. It does not create the directory or verify that the file exists; it only constructs a path.

Create a directory using `Directory.CreateDirectory`:

```csharp
Directory.CreateDirectory(directory);
```

This method creates the directory and any missing parent directories. If the directory already exists, the call succeeds without needing a separate existence check.

A reliable program should decide explicitly where its files belong. For a learning exercise, using a `data` directory beneath the current working directory is acceptable. For deployed applications, the appropriate location depends on whether the data is application content, user data, or configuration.

### 3. Writing and reading text files

The `File` class offers several common methods:

- `File.WriteAllText(path, content)` writes a string and replaces existing file contents.
- `File.AppendAllText(path, content)` appends text to an existing file or creates it.
- `File.ReadAllText(path)` reads the entire file as a string.
- `File.WriteAllLines(path, lines)` writes a sequence of strings, one line at a time.
- `File.ReadAllLines(path)` reads all lines into a string array.

Example:

```csharp
string path = Path.Combine(
    Directory.GetCurrentDirectory(),
    "boid-counts.txt");

string[] counts = { "10", "25", "50" };

File.WriteAllLines(path, counts);

string[] loadedCounts = File.ReadAllLines(path);

foreach (string count in loadedCounts)
{
    Console.WriteLine(count);
}
```

The result is a text file containing three lines.

Notice that `ReadAllLines` returns strings. If the file represents numeric data, you must parse those strings into the appropriate numeric types and handle invalid values.

For larger files, streaming is often preferable to reading the entire file at once. A `StreamReader` can process lines incrementally:

```csharp
using var reader = new StreamReader(path);

string? line;

while ((line = reader.ReadLine()) is not null)
{
    Console.WriteLine(line);
}
```

`ReadLine` returns `null` when the end of the file is reached. The nullable `string?` declaration communicates that possibility to the compiler.

### 4. Resource management with `using`

Some file APIs use operating-system resources that should be released when they are no longer needed. Streams, readers, and writers implement `IDisposable`.

The `using` statement ensures that a disposable resource is disposed of when execution leaves its scope, including when an exception occurs:

```csharp
using (StreamWriter writer = new StreamWriter("log.txt"))
{
    writer.WriteLine("Simulation started");
}
```

C# also supports the using declaration:

```csharp
using StreamWriter writer = new StreamWriter("log.txt");

writer.WriteLine("Simulation started");
```

With a using declaration, disposal happens when execution leaves the containing scope. It does not happen immediately after the final write statement.

Disposal flushes and releases the writer's resources. For stream-based operations, failing to dispose resources can leave files open longer than necessary and can prevent buffered content from being written promptly.

The simpler `File.ReadAllText` and `File.WriteAllText` methods manage their underlying resources internally. You do not need to wrap those calls in an additional `using` statement.

### 5. Why serialization is useful

Serialization converts an in-memory object into a representation that can be stored or transmitted. Deserialization reconstructs an object from that representation.

JSON (JavaScript Object Notation) is a common text format for structured data. It represents objects as named properties and supports strings, numbers, Booleans, arrays, and nested objects.

Suppose a simulation has this configuration:

```csharp
public sealed class SimulationConfig
{
    public int BoidCount { get; set; } = 100;
    public double MovementSpeed { get; set; } = 2.0;
    public bool IsRunning { get; set; }
}
```

A `SimulationConfig` object can be converted to JSON using `System.Text.Json`, which is included in modern .NET:

```csharp
using System.Text.Json;

SimulationConfig config = new()
{
    BoidCount = 150,
    MovementSpeed = 2.5,
    IsRunning = false
};

string json = JsonSerializer.Serialize(config);

Console.WriteLine(json);
```

The JSON output is similar to:

```json
{"BoidCount":150,"MovementSpeed":2.5,"IsRunning":false}
```

JSON is text, so it can be written to a file, inspected in an editor, or transferred to another application.

### 6. Formatting JSON

Compact JSON is efficient to store but less convenient for humans to read. `JsonSerializerOptions` can configure serialization behavior, including indentation:

```csharp
using System.Text.Json;

JsonSerializerOptions options = new()
{
    WriteIndented = true
};

string json = JsonSerializer.Serialize(config, options);

Console.WriteLine(json);
```

The output is formatted across multiple lines:

```json
{
  "BoidCount": 150,
  "MovementSpeed": 2.5,
  "IsRunning": false
}
```

You can reuse the same options object for multiple serialization operations. For a small application, default serialization is often sufficient; indentation is useful when people need to inspect the file.

By default, `System.Text.Json` serializes public properties. Public fields are not included by default. This is one reason configuration types commonly use properties.

### 7. Saving an object to JSON

Serialization and file I/O can be combined to persist an object:

```csharp
using System.IO;
using System.Text.Json;

string dataDirectory = Path.Combine(
    Directory.GetCurrentDirectory(),
    "data");

Directory.CreateDirectory(dataDirectory);

string configPath = Path.Combine(
    dataDirectory,
    "config.json");

SimulationConfig config = new()
{
    BoidCount = 150,
    MovementSpeed = 2.5,
    IsRunning = false
};

JsonSerializerOptions options = new()
{
    WriteIndented = true
};

string json = JsonSerializer.Serialize(config, options);

File.WriteAllText(configPath, json);

Console.WriteLine($"Saved configuration to: {configPath}");
```

The directory is created before writing the file, so the operation does not fail merely because the `data` directory is missing.

Remember that `WriteAllText` overwrites an existing file. If preserving previous data matters, use an explicit backup or update strategy rather than assuming a write is non-destructive.

### 8. Loading JSON into an object

Deserialization converts JSON text back into a C# object:

```csharp
string json = File.ReadAllText(configPath);

SimulationConfig? loadedConfig =
    JsonSerializer.Deserialize<SimulationConfig>(json);

if (loadedConfig is not null)
{
    Console.WriteLine($"Boid count: {loadedConfig.BoidCount}");
    Console.WriteLine($"Movement speed: {loadedConfig.MovementSpeed}");
}
```

`Deserialize<T>` returns an instance of the requested type when deserialization succeeds. It can also return `null`, for example when the JSON text represents the JSON value `null`. The nullable variable reflects that possibility.

Deserialization does not automatically guarantee that the values are valid for your application. A JSON file could contain a negative boid count or an invalid movement speed. After loading, validate the data before using it.

Also, JSON property names must be compatible with the serializer's configured naming behavior. By default, `System.Text.Json` uses case-sensitive property-name matching unless configured otherwise. You can control this with serializer options.

### 9. Handling file and JSON errors

File operations can fail for reasons outside your program's control. The file might not exist, the process might lack permission, a device might become unavailable, or the path might be invalid.

JSON parsing can also fail if the file contains malformed JSON. `JsonSerializer.Deserialize` can throw `JsonException` for invalid JSON or incompatible data.

Handle expected failure cases deliberately:

```csharp
try
{
    string json = File.ReadAllText(configPath);

    SimulationConfig? loadedConfig =
        JsonSerializer.Deserialize<SimulationConfig>(json);

    if (loadedConfig is null)
    {
        Console.WriteLine("The configuration file contains no object.");
    }
    else
    {
        Console.WriteLine(
            $"Loaded {loadedConfig.BoidCount} boids.");
    }
}
catch (FileNotFoundException)
{
    Console.WriteLine("The configuration file was not found.");
}
catch (JsonException)
{
    Console.WriteLine("The configuration file contains invalid JSON.");
}
catch (IOException ex)
{
    Console.WriteLine($"A file operation failed: {ex.Message}");
}
catch (UnauthorizedAccessException)
{
    Console.WriteLine("The application does not have file access.");
}
```

The order of these handlers matters: specific exceptions should be handled before broader categories that might catch them. `FileNotFoundException` is a type of `IOException`, so it belongs before the general `IOException` handler.

Do not catch every exception just to hide errors. Handle the failures your application can reasonably respond to, and allow unexpected failures to remain visible during development.

For a configuration file, a missing file might mean the application should create a default configuration. A malformed file might require a clear error message or a backup recovery strategy. The right response depends on the role of the data.

### 10. Asynchronous file I/O

Asynchronous I/O allows an application to await file operations without synchronously blocking the executing thread while the operation is pending. This can be particularly useful in applications that handle user interaction or many concurrent operations.

C# uses `async` and `await` to work with asynchronous methods:

```csharp
string content = await File.ReadAllTextAsync(configPath);
Console.WriteLine(content);
```

Writing asynchronously is similar:

```csharp
await File.WriteAllTextAsync(configPath, json);
```

A method containing `await` generally needs the `async` modifier and an appropriate asynchronous return type. Top-level statements in a modern .NET console application support `await`, so these examples can be used directly in `Program.cs`.

Asynchronous I/O does not necessarily make an individual file operation finish faster. It allows the program to avoid waiting synchronously for the operation to complete, which is valuable when responsiveness or concurrency matters.

Use asynchronous file APIs when they suit the application and its calling context. For very small, short-lived console exercises, synchronous file operations may be simpler and entirely adequate.

### 11. Asynchronous JSON persistence

`System.Text.Json` provides asynchronous serialization and deserialization methods that work with streams. These are useful when working with larger JSON documents or when combining serialization with asynchronous file access.

For example, you can serialize directly to a file stream:

```csharp
await using FileStream stream = File.Create(configPath);

await JsonSerializer.SerializeAsync(stream, config, options);
```

Here, `File.Create` returns a `FileStream`, and `await using` ensures asynchronous disposal when the scope ends. `FileStream` supports asynchronous disposal, and this pattern is appropriate when using its asynchronous APIs.

To deserialize from a file:

```csharp
await using FileStream stream = File.OpenRead(configPath);

SimulationConfig? loadedConfig =
    await JsonSerializer.DeserializeAsync<SimulationConfig>(stream);
```

These operations avoid manually creating an intermediate JSON string. For small configuration files, serializing to a string and writing it with `File.WriteAllTextAsync` can still be perfectly reasonable.

### 12. Data validation and safe persistence

A valid JSON document is not necessarily valid application data. Serialization preserves values; it does not automatically enforce every rule your application requires.

For example, a configuration might require:

- A non-negative boid count.
- A finite, non-negative movement speed.
- A valid simulation width and height.

After deserialization, validate these constraints before applying the configuration. If the values are invalid, reject the configuration or replace it with a documented default.

Avoid saving secrets such as passwords, private tokens, or credentials in ordinary configuration files unless you have an appropriate secure storage strategy. File permissions and storage locations matter, particularly when data belongs to a specific user.

Finally, consider what happens if the application stops during a write. For important data, a safer update strategy may write to a temporary file and then replace the destination after the write succeeds. The simplest approach is suitable for this module, but more robust persistence may be needed as the application grows.

### 13. Applying file I/O to the Boids simulation

The Boids simulation will eventually need to retain configuration between runs. A `SimulationConfig` object can represent those settings, JSON can provide a human-readable representation, and file I/O can save and restore it.

The responsibilities can be kept distinct:

- The configuration class represents the settings and their valid state.
- Serialization converts the configuration to or from JSON.
- File operations read or write the JSON at a known path.
- The application decides what to do when the file is missing, invalid, or inaccessible.

This separation makes the code easier to test and maintain. It also makes it possible to replace file-based persistence later without rewriting the simulation's core behavior.

## Challenge

Build a console application that saves and loads a Boids simulation configuration using JSON.

Your program must satisfy all of the following requirements:

1. **Define a configuration class:** Create a `SimulationConfig` class with a boid count, movement speed, and running state.
2. **Set sensible defaults:** Give the configuration reasonable initial values so the application can recover when no saved file exists.
3. **Create a data directory:** Use `Directory.CreateDirectory` and `Path.Combine` to create and locate a `data/config.json` file relative to the application's current working directory.
4. **Serialize to JSON:** Use `System.Text.Json` to serialize a configuration object with readable indentation.
5. **Save asynchronously:** Write the JSON configuration using an asynchronous file API.
6. **Load asynchronously:** Read the saved file and deserialize it into a `SimulationConfig` object.
7. **Validate loaded data:** Reject a negative boid count or a negative or non-finite movement speed before using the loaded configuration.
8. **Handle expected errors:** Handle a missing file and malformed JSON without terminating unexpectedly. Provide a clear message and use default configuration values when appropriate.
9. **Demonstrate persistence:** Save a configuration, load it again, and print the loaded values so that the result can be checked.
10. **Separate responsibilities:** Keep configuration data in the configuration class and place reusable save/load or validation logic in focused methods rather than putting all logic in the top-level statements.

# Resource Management

## Micro Goal

Understand how .NET manages resources, why some resources require explicit cleanup, and how to use `IDisposable`, `using`, and `try`/`finally` to release resources safely and reliably.

## Concepts Covered

- Managed memory versus unmanaged resources
- Garbage collection and object lifetime
- `IDisposable` and the `Dispose()` method
- The `using` statement and `using` declarations
- Deterministic cleanup with `try`/`finally`
- `using` with streams and file operations
- `StreamReader` and `StreamWriter`
- Resource ownership and disposal responsibilities
- Exception safety during resource cleanup
- `IAsyncDisposable` and `await using`
- The difference between disposing an object and setting a reference to `null`
- Common resource management mistakes

## Completion Criteria

- [ ] Explain the difference between garbage collection and resource disposal.
- [ ] Explain why some objects implement `IDisposable`.
- [ ] Use a `using` statement and a `using` declaration correctly.
- [ ] Explain how `using` guarantees disposal when an exception occurs.
- [ ] Read from and write to a file using disposable streams.
- [ ] Implement `IDisposable` for a simple class that owns a disposable resource.
- [ ] Avoid disposing resources owned by another component.
- [ ] Explain when `IAsyncDisposable` and `await using` are appropriate.
- [ ] Complete the challenge with correct cleanup, including when an operation fails.

## Lecture

### 1. Managed memory and resource management

.NET automatically manages memory for most objects through the garbage collector (GC). When objects are no longer reachable from application code, the garbage collector can eventually reclaim their managed memory.

However, not every resource is simply a block of managed memory. Applications commonly interact with resources such as:

- Open files and file handles
- Network connections
- Database connections
- Streams
- Operating system handles
- Native resources allocated outside the managed runtime

These resources may need to be released promptly, independently of when garbage collection occurs.

For example, an application that opens many files without closing them may exhaust the operating system's available file handles. Waiting for garbage collection is not a reliable way to prevent that problem.

**Garbage collection manages memory; resource disposal releases resources that require explicit cleanup.** These are related but distinct responsibilities.

### 2. The `IDisposable` interface

The `IDisposable` interface defines a single method:

```csharp
public interface IDisposable
{
    void Dispose();
}
```

A class implements `IDisposable` when it provides a way to release resources or perform other cleanup that should happen deterministically.

Many .NET resource types implement this interface, including `FileStream`, `StreamReader`, `StreamWriter`, and many database connection types.

For example:

```csharp
FileStream stream = File.OpenRead("data.txt");

// Use the stream.

stream.Dispose();
```

Calling `Dispose()` signals that the resource is no longer needed and should be cleaned up. After disposal, the object generally must not be used for its original resource operations. The precise behavior of subsequent calls depends on the type.

This direct pattern has a weakness: if an exception occurs before `Dispose()` is called, cleanup can be skipped.

```csharp
FileStream stream = File.OpenRead("data.txt");

ProcessFile(stream); // If this throws, Dispose may never run.

stream.Dispose();
```

The problem is not that `Dispose()` is ineffective. The problem is that the control flow does not guarantee it will be reached.

### 3. The `using` statement

The `using` statement ensures that a disposable resource is cleaned up when execution leaves the statement's scope, including when an exception occurs.

```csharp
using (FileStream stream = File.OpenRead("data.txt"))
{
    Console.WriteLine($"File size: {stream.Length} bytes");
}
```

When execution leaves the block, the runtime-generated cleanup logic calls `Dispose()` on the stream. You do not need to write a separate cleanup call at the end.

This remains true if the body throws an exception. The exception still propagates unless it is handled, but the stream is disposed during the exit from the block.

The `using` statement is the standard choice when a resource's lifetime should be limited to a specific block of code.

### 4. The `using` declaration

C# also supports a `using` declaration, which disposes the resource when execution leaves the enclosing scope.

```csharp
using FileStream stream = File.OpenRead("data.txt");

Console.WriteLine($"File size: {stream.Length} bytes");
```

Unlike the `using` statement, this form does not introduce an additional block. The resource remains available until execution leaves the surrounding scope.

This is often convenient in a method with a straightforward sequence of operations.

Compare the two forms:

```csharp
static void ReadFile(string path)
{
    using (FileStream stream = File.OpenRead(path))
    {
        Console.WriteLine(stream.Length);
    }

    // The stream has been disposed here.
}
```

```csharp
static void ReadFile(string path)
{
    using FileStream stream = File.OpenRead(path);

    Console.WriteLine(stream.Length);

    // The stream is disposed when this method's scope exits.
}
```

Both are valid. Use the block form when the resource should be released before the surrounding scope ends. Use the declaration form when keeping the resource alive until the end of the current scope is appropriate.

### 5. Reading files safely

The `File` class provides convenient methods for working with files. For larger files or incremental processing, streams allow data to be read without loading the entire file into memory.

`StreamReader` is designed for reading text. It implements `IDisposable`, so it should normally be used with `using`.

```csharp
using StreamReader reader = new StreamReader("data.txt");

string? line;

while ((line = reader.ReadLine()) is not null)
{
    Console.WriteLine(line);
}
```

`ReadLine()` returns the next line, or `null` when the end of the file is reached. The variable is therefore declared as `string?`, which communicates that it may contain `null`.

You can also use `File.OpenText`:

```csharp
using StreamReader reader = File.OpenText("data.txt");

string contents = reader.ReadToEnd();

Console.WriteLine(contents);
```

`ReadToEnd()` loads the remaining contents into a string. That is convenient for small files, but it may consume substantial memory for large files. Reading incrementally is generally more appropriate when processing large amounts of data.

The `using` declaration ensures the reader is disposed when the scope ends, even if reading or processing the contents throws an exception.

### 6. Writing files safely

`StreamWriter` provides methods for writing text to a stream. It also implements `IDisposable`.

```csharp
using StreamWriter writer = new StreamWriter("output.txt");

writer.WriteLine("Boids simulation");
writer.WriteLine("Boid count: 100");
```

Disposing the writer flushes buffered output and releases its underlying resources. Without proper cleanup, buffered data may not be written as expected before the application exits.

For straightforward file operations, the `File` class can simplify the code:

```csharp
File.WriteAllText("output.txt", "Boids simulation");

File.AppendAllText("output.txt", Environment.NewLine + "Boid count: 100");

string contents = File.ReadAllText("output.txt");

Console.WriteLine(contents);
```

These methods manage the underlying file resources internally. You do not need to dispose a stream that you did not create or receive as a disposable object.

`File.ReadAllText` and `File.WriteAllText` are convenient for small files. Explicit streams are useful when you need incremental access, control over the stream lifetime, or more advanced I/O behavior.

### 7. Resource ownership

A central rule of resource management is: **the component responsible for owning a resource is responsible for disposing it.**

Suppose a class creates a stream internally and keeps it as a field. That class owns the stream and should dispose it when the class is disposed.

```csharp
sealed class TextFileProcessor : IDisposable
{
    private readonly StreamReader _reader;

    public TextFileProcessor(string path)
    {
        _reader = new StreamReader(path);
    }

    public string? ReadLine()
    {
        return _reader.ReadLine();
    }

    public void Dispose()
    {
        _reader.Dispose();
    }
}
```

Usage:

```csharp
using TextFileProcessor processor = new TextFileProcessor("data.txt");

string? line = processor.ReadLine();

Console.WriteLine(line ?? "No line available.");
```

`TextFileProcessor` owns the reader because it creates it and retains responsibility for its lifetime. Disposing the processor disposes the reader.

By contrast, a method that merely receives a stream from its caller generally should not dispose that stream unless the API explicitly transfers ownership.

```csharp
static long GetStreamLength(Stream stream)
{
    return stream.Length;
}
```

This method uses the stream but does not own it. Disposing it would unexpectedly prevent the caller from using the stream afterward.

Resource ownership should be explicit in a design. If a class creates a resource, it usually owns it. If a class merely borrows a resource from a caller, it should generally leave cleanup to that caller.

### 8. Implementing `IDisposable` correctly

Implement `IDisposable` when your class owns disposable resources and needs to expose a cleanup operation.

A simple implementation can directly dispose an owned resource:

```csharp
sealed class TextFileProcessor : IDisposable
{
    private readonly StreamReader _reader;

    public TextFileProcessor(string path)
    {
        _reader = new StreamReader(path);
    }

    public string? ReadLine()
    {
        return _reader.ReadLine();
    }

    public void Dispose()
    {
        _reader.Dispose();
    }
}
```

The class is `sealed` because this example is deliberately not designed for inheritance. The `readonly` field ensures the reference cannot be reassigned after construction.

For a class that owns multiple disposable resources, dispose each owned resource. If one cleanup operation might throw, design cleanup carefully so that another resource is not accidentally left undisposed.

Do not implement `IDisposable` just because your class exists or stores ordinary managed data. A class that only contains strings, integers, lists, and other managed objects that it does not own as disposable resources usually does not need its own `Dispose()` method.

For more complex classes, especially those that inherit from other disposable types or directly own unmanaged resources, additional disposal patterns may be appropriate. This module focuses on the common case of managing other managed objects that implement `IDisposable`.

### 9. `try` and `finally`

The `using` construct is preferred for most ordinary disposal scenarios, but it is useful to understand the mechanism behind it.

A `finally` block executes when control leaves a corresponding `try` block, whether normally or because an exception is thrown.

```csharp
FileStream? stream = null;

try
{
    stream = File.OpenRead("data.txt");

    Console.WriteLine($"File size: {stream.Length} bytes");
}
finally
{
    stream?.Dispose();
}
```

The null-conditional operator `?.` calls `Dispose()` only if the stream was successfully assigned a non-null reference.

The `finally` block still executes if reading the stream fails. It also executes if opening the file fails, in which case the stream remains `null`.

This pattern can be useful when cleanup logic is more complicated than a simple `using` declaration. For a single disposable resource, however, `using` is usually clearer and less error-prone.

### 10. Disposal is not the same as setting a reference to `null`

Consider:

```csharp
FileStream stream = File.OpenRead("data.txt");

stream.Dispose();
```

The variable `stream` still refers to the `FileStream` object. The object has been disposed, but the reference itself has not disappeared.

Setting the variable to `null` is a separate operation:

```csharp
stream = null!;
```

The null-forgiving operator suppresses a compiler warning in this example; it does not dispose the object or make the previous operation safer. This assignment is shown only to emphasize the distinction and is not a recommended cleanup technique.

Disposal is about releasing resources. Setting a reference to `null` only changes that variable's reference. Garbage collection may eventually reclaim the object's managed memory when the object is no longer reachable, but that does not replace deterministic disposal.

After disposal, do not continue using an object as if its resource were still available.

### 11. Asynchronous disposal with `IAsyncDisposable`

Some resources need asynchronous cleanup. For example, a resource may need to perform asynchronous operations while shutting down or releasing an underlying connection.

.NET provides the `IAsyncDisposable` interface:

```csharp
public interface IAsyncDisposable
{
    ValueTask DisposeAsync();
}
```

Use `await using` when the object implements `IAsyncDisposable` and its asynchronous cleanup should be awaited.

```csharp
await using var resource = new SomeAsyncDisposableResource();

// Use the resource.
```

`SomeAsyncDisposableResource` here represents a type that actually implements `IAsyncDisposable`; it is illustrative rather than a built-in .NET class.

The asynchronous disposal operation runs when the scope exits, and the compiler generates the necessary asynchronous cleanup logic.

Some types implement both `IDisposable` and `IAsyncDisposable`. Choose the disposal form that matches the API and context. Asynchronous disposal is not automatically better for every resource, and it does not mean every file operation must be asynchronous.

In a console application using top-level statements, `await` is supported directly:

```csharp
await using var stream = new SomeAsyncDisposableResource();
```

This example still requires a real type named `SomeAsyncDisposableResource` to compile. The important syntax is `await using`, which awaits asynchronous disposal.

### 12. Common resource management mistakes

Avoid these common errors:

- **Relying on garbage collection to close resources:** Collection timing is nondeterministic. Use deterministic disposal for disposable resources.
- **Forgetting cleanup when an exception occurs:** Use `using` or `await using` instead of relying on a cleanup statement at the end of a method.
- **Disposing a borrowed resource:** Do not dispose an object owned by the caller unless ownership has been explicitly transferred.
- **Continuing to use a disposed object:** Disposal may release the underlying resource and make further operations invalid.
- **Implementing `IDisposable` unnecessarily:** Ordinary managed data alone does not normally require a custom disposal method.
- **Loading large files entirely into memory:** Choose streaming operations when incremental processing is more appropriate.
- **Confusing synchronous and asynchronous cleanup:** Use `await using` for asynchronous disposal when supported and appropriate.
- **Assuming `Dispose()` means the reference is null:** The reference remains unless you change it separately.

The central principle is to make resource lifetimes explicit. Create or acquire a resource, use it while it is valid, and ensure its owner disposes it when it is no longer needed.

## Challenge

Build a console program that writes simulation data to a file and reads it back while demonstrating safe resource management.

Your program must satisfy all of the following requirements:

1. **Write a file:** Use `StreamWriter` or another disposable stream to write a simulation title and at least three lines of boid data to a text file.
2. **Dispose deterministically:** Use `using` or a `using` declaration for every disposable resource you create or acquire.
3. **Read the file:** Read the saved data back using `StreamReader`, print each line, and handle the end of the file correctly.
4. **Implement a resource-owning class:** Create a small class named `SimulationFileReader` that owns a `StreamReader` and implements `IDisposable`.
5. **Demonstrate ownership:** Have `SimulationFileReader` dispose the reader it owns. Use the class with `using` in the top-level code.
6. **Handle failures:** Handle a file-related exception at an appropriate boundary, such as a missing file or an access failure, and print a useful error message. Do not silently swallow exceptions.
7. **Avoid disposing borrowed resources:** Do not dispose a resource in a method or class that does not own it.
8. **Keep responsibilities clear:** The file-writing operation, the resource-owning reader, and the top-level demonstration should have separate, straightforward responsibilities.
9. **Print clear output:** Label the writing, reading, and error-handling portions so the behavior is easy to follow.

Use a relative file path in a dedicated subdirectory or another predictable location. Ensure the directory exists before writing the file. Keep the code focused on resource lifetime and cleanup rather than adding unrelated abstractions.

After completing the challenge, verify that the file is written, read successfully, and that the program still cleans up its resources if a file operation throws an exception.
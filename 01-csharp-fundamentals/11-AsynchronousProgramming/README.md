# Asynchronous Programming

## Micro Goal

Understand how C# asynchronous programming works, use `async` and `await` to handle operations without unnecessarily blocking a thread, and coordinate multiple asynchronous operations safely and efficiently.

## Concepts Covered

- Synchronous versus asynchronous execution
- `Task` and `Task<T>`
- `async` and `await`
- Asynchronous method signatures and return values
- `Task.Delay` versus `Thread.Sleep`
- I/O-bound versus CPU-bound work
- Running multiple operations concurrently with `Task.WhenAll`
- Sequential versus concurrent asynchronous operations
- Exception handling in asynchronous methods
- Cancellation with `CancellationToken`
- `Task.Run` for CPU-bound work
- Basic thread-safety considerations
- Asynchronous programming in .NET applications

## Completion Criteria

- [ ] Explain the difference between synchronous and asynchronous execution.
- [ ] Explain what `Task` and `Task<T>` represent.
- [ ] Write and call an asynchronous method using `async` and `await`.
- [ ] Return a value from an asynchronous method using `Task<T>`.
- [ ] Explain why `Task.Delay` is preferable to `Thread.Sleep` in asynchronous workflows.
- [ ] Run independent asynchronous operations concurrently with `Task.WhenAll`.
- [ ] Handle exceptions from awaited tasks.
- [ ] Pass and honor a `CancellationToken`.
- [ ] Distinguish I/O-bound work from CPU-bound work.
- [ ] Use `Task.Run` appropriately for CPU-bound work without treating it as a universal performance improvement.
- [ ] Explain why shared mutable state requires care when work can execute concurrently.
- [ ] Complete the challenge with a working asynchronous console program.

## Lecture

### 1. Synchronous and asynchronous execution

Synchronous code executes an operation and waits for it to finish before proceeding to the next statement.

```csharp
Console.WriteLine("Starting");
PerformWork();
Console.WriteLine("Finished");

static void PerformWork()
{
    Thread.Sleep(2000);
}
```

`Thread.Sleep(2000)` blocks the current thread for approximately two seconds. The program cannot execute the next statement on that thread until the sleep finishes.

Asynchronous programming allows an operation to be started and awaited without blocking the current thread while the operation is waiting. This is especially useful when an application must remain responsive or handle many operations that spend time waiting for external resources.

```csharp
Console.WriteLine("Starting");
await PerformWorkAsync();
Console.WriteLine("Finished");

static async Task PerformWorkAsync()
{
    await Task.Delay(2000);
}
```

`Task.Delay` represents an asynchronous delay. While the delay is pending, the current thread is available to do other work. When the delay completes, execution resumes after the `await`.

The output order remains `Starting` followed by `Finished`, but the thread does not have to sit idle during the delay.

Asynchronous execution does not automatically mean that work runs on another thread, nor does it guarantee that an operation finishes faster. Its main advantage is allowing a program to make progress without blocking a thread while waiting.

### 2. Understanding `Task` and `Task<T>`

A `Task` represents an asynchronous operation that can complete in the future. It can complete successfully, fail with an exception, or be canceled.

A `Task<T>` represents an asynchronous operation that eventually produces a result of type `T`.

For example:

```csharp
static async Task WaitAsync()
{
    await Task.Delay(500);
}

static async Task<int> GetBoidCountAsync()
{
    await Task.Delay(500);
    return 100;
}
```

`WaitAsync` returns a `Task` because it does not produce a value. `GetBoidCountAsync` returns a `Task<int>` because its result is an integer.

Call these methods with `await`:

```csharp
await WaitAsync();

int boidCount = await GetBoidCountAsync();

Console.WriteLine(boidCount);
```

The first `await` waits for completion. The second waits for completion and obtains the integer result.

A task is not necessarily a thread. It represents the state and eventual outcome of an operation. For I/O operations, a task can remain pending while the operating system or a remote service performs work, without requiring a dedicated thread to remain blocked for the entire wait.

### 3. The `async` and `await` keywords

The `async` modifier allows a method to use `await` and tells the compiler to build the machinery needed to suspend and resume that method.

The `await` operator waits asynchronously for an awaitable operation to complete. If the operation has already completed, execution can continue immediately. Otherwise, the method can suspend and resume later.

A typical asynchronous method has this form:

```csharp
static async Task DoSomethingAsync()
{
    // Perform synchronous work.
    await Task.Delay(1000);

    // Continue after the delay completes.
}
```

A method that returns a result uses `Task<T>`:

```csharp
static async Task<double> CalculateSpeedAsync(
    double baseSpeed,
    double multiplier)
{
    await Task.Delay(100);

    return baseSpeed * multiplier;
}
```

Call it like this:

```csharp
double speed = await CalculateSpeedAsync(2.0, 1.5);

Console.WriteLine(speed);
```

For ordinary application code, the common asynchronous return types are:

- `Task` for an operation with no returned value.
- `Task<T>` for an operation that returns a value.
- `void` for event handlers that must follow a `void` signature, such as some UI event handlers.

Avoid using `async void` for ordinary methods. Callers cannot await an `async void` method, and its exceptions cannot be handled through a returned task in the normal way. Use `Task` or `Task<T>` whenever possible.

An asynchronous method does not need to use `async` if it simply returns an existing task:

```csharp
static Task WaitAsync()
{
    return Task.Delay(1000);
}
```

This is valid because the method returns the task directly and does not need to await it or perform additional work afterward.

### 4. Top-level statements and asynchronous code

Modern C# console projects support top-level statements. You can use `await` directly in top-level code without declaring a `Main` method yourself.

For example:

```csharp
Console.WriteLine("Starting simulation setup");

int boidCount = await GetBoidCountAsync();

Console.WriteLine($"Boids: {boidCount}");

static async Task<int> GetBoidCountAsync()
{
    await Task.Delay(500);
    return 100;
}
```

The compiler generates the program entry point. This makes short asynchronous exercises straightforward.

When you place methods in a separate class, ordinary class methods can use `async Task` or `async Task<T>` as needed. Keep the entry-point code focused on coordinating the operations and put reusable logic in appropriately named methods.

### 5. Why `Task.Delay` is different from `Thread.Sleep`

`Thread.Sleep` blocks the current thread for a specified duration:

```csharp
Thread.Sleep(1000);
```

`Task.Delay` returns a task that completes after a delay:

```csharp
await Task.Delay(1000);
```

The difference matters in applications that need to remain responsive or process many concurrent operations. Blocking a thread unnecessarily consumes a resource that could otherwise serve other work.

For asynchronous workflows, prefer `Task.Delay` when you genuinely need an asynchronous delay. Use `Thread.Sleep` only when deliberately blocking a thread is appropriate, which is uncommon in normal asynchronous application code.

Neither delay is a precise real-time scheduling mechanism. The continuation after a delay may run later than the requested duration because of scheduling and system load.

A delay is useful for learning and simulation timing, but a real Boids simulation should generally use an explicit simulation time step rather than relying on delays to define the physics.

### 6. Sequential asynchronous operations

Consider two independent operations:

```csharp
static async Task<string> LoadPositionAsync()
{
    await Task.Delay(1000);
    return "Position loaded";
}

static async Task<string> LoadVelocityAsync()
{
    await Task.Delay(1000);
    return "Velocity loaded";
}
```

Awaiting them one after another makes the operations sequential:

```csharp
string position = await LoadPositionAsync();
string velocity = await LoadVelocityAsync();

Console.WriteLine(position);
Console.WriteLine(velocity);
```

Each operation is started when its method is called. The second method is not called until the first awaited operation completes. With these artificial one-second delays, the total elapsed time will be approximately two seconds, subject to scheduling overhead.

Sequential execution is appropriate when the second operation depends on the result of the first. It is unnecessary when the operations are independent and can safely proceed at the same time.

### 7. Concurrent asynchronous operations with `Task.WhenAll`

If operations are independent, start them before awaiting their combined completion:

```csharp
Task<string> positionTask = LoadPositionAsync();
Task<string> velocityTask = LoadVelocityAsync();

string[] results = await Task.WhenAll(
    positionTask,
    velocityTask);

Console.WriteLine(results[0]);
Console.WriteLine(results[1]);
```

Both methods are called before the program awaits `Task.WhenAll`. Their delays can overlap, so the total elapsed time is approximately one second rather than two.

`Task.WhenAll` returns a task that completes when all supplied tasks have completed. When the inputs are `Task<T>`, the result contains the results in the same order as the supplied tasks.

This is concurrency, not necessarily parallel execution. The operations overlap in time, but they may not be running on separate CPU threads.

Use `Task.WhenAll` when several independent operations should all complete before the program proceeds. Do not use it blindly for dependent operations, and do not start unbounded numbers of tasks without considering resource limits.

### 8. Exception handling in asynchronous methods

An asynchronous operation can fail. When an awaited task completes with an exception, `await` propagates that exception at the point where the task is awaited.

Use ordinary `try` and `catch` blocks:

```csharp
try
{
    int result = await GetBoidCountAsync();
    Console.WriteLine($"Boid count: {result}");
}
catch (InvalidOperationException exception)
{
    Console.WriteLine($"Operation failed: {exception.Message}");
}
```

Catch exceptions you can meaningfully handle. Avoid catching every exception merely to hide failures.

When awaiting `Task.WhenAll`, an exception is propagated if one or more tasks fail. The combined task does not complete successfully unless all supplied tasks succeed. If you need to inspect individual outcomes, retain references to the individual tasks and inspect their status or handle each operation's failure deliberately.

For example:

```csharp
Task<string> firstTask = LoadPositionAsync();
Task<string> secondTask = LoadVelocityAsync();

try
{
    await Task.WhenAll(firstTask, secondTask);
    Console.WriteLine(firstTask.Result);
    Console.WriteLine(secondTask.Result);
}
catch (Exception exception)
{
    Console.WriteLine($"At least one operation failed: {exception.Message}");
}
```

Here, reading `Result` after `WhenAll` completes successfully does not block because both tasks are already complete. In general, prefer `await task` to `.Result` or `.Wait()` because synchronous blocking can undermine asynchronous code and can cause deadlocks in some synchronization contexts.

### 9. Cancellation with `CancellationToken`

Cancellation allows a caller to request that an operation stop. Cancellation in .NET is cooperative: the operation must observe the request and respond to it.

A `CancellationToken` communicates the request to the operation. A `CancellationTokenSource` can issue the request.

```csharp
using var cancellationSource = new CancellationTokenSource();

Task operation = RunSimulationAsync(cancellationSource.Token);

// Request cancellation.
cancellationSource.Cancel();

try
{
    await operation;
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation canceled.");
}

static async Task RunSimulationAsync(
    CancellationToken cancellationToken)
{
    for (int step = 1; step <= 10; step++)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Console.WriteLine($"Step {step}");

        await Task.Delay(200, cancellationToken);
    }
}
```

The token is checked before each step, and it is also passed to `Task.Delay`. If cancellation is requested, the delay can end early by throwing an `OperationCanceledException` or a derived exception.

In a real application, cancellation can originate from a user pressing a stop button, navigating away from a page, or shutting down an application.

A cancellation request does not forcibly terminate arbitrary code. Long-running operations should check the token regularly and pass it to APIs that support cancellation.

### 10. I/O-bound versus CPU-bound work

It is important to distinguish two kinds of work.

**I/O-bound work** spends much of its time waiting for an external operation, such as a network response, a file read, or a database query. Asynchronous APIs allow the program to avoid blocking a thread during that wait.

Examples include:

- Downloading data from a web service.
- Reading a file asynchronously.
- Waiting for a database query to complete.

For I/O-bound work, use the asynchronous API directly:

```csharp
string contents = await File.ReadAllTextAsync("settings.json");
```

This lets the application wait for file I/O without unnecessarily blocking its current thread.

**CPU-bound work** spends time performing calculations. A Boids simulation that calculates distances, neighborhoods, and steering forces is CPU-bound.

Making a CPU-intensive method `async` does not automatically make its computation asynchronous or faster. If the work is performed synchronously before the method returns, it still consumes CPU time on the calling thread.

For example:

```csharp
static double CalculateTotalDistance(double[] distances)
{
    double total = 0;

    foreach (double distance in distances)
    {
        total += distance;
    }

    return total;
}
```

This method is synchronous because it performs its computation directly and returns a result.

For a sufficiently expensive CPU-bound operation that should not run on the current thread, `Task.Run` can schedule the computation on the thread pool:

```csharp
double total = await Task.Run(
    () => CalculateTotalDistance(distances));
```

`Task.Run` is not necessary for every calculation. Small computations are often best executed directly. For large workloads, measure performance and consider allocation costs, parallel algorithms, and the number of available CPU cores.

Do not wrap naturally asynchronous I/O in `Task.Run` simply to make it appear asynchronous. Use the I/O API's asynchronous method instead.

### 11. Shared state and concurrency

Concurrent operations can create problems when they modify the same mutable data.

Consider two operations that both increment the same integer. An increment is a read-modify-write sequence, so concurrent increments can interfere with each other if no synchronization is used.

A `List<T>` is also not generally safe for unrestricted concurrent modification. Multiple operations that add, remove, or iterate over the same list can cause data races or invalid enumeration.

A safer starting point is to give each operation its own data and combine results after the operations finish:

```csharp
Task<int> firstTask = CalculateCountAsync(100);
Task<int> secondTask = CalculateCountAsync(200);

int[] counts = await Task.WhenAll(firstTask, secondTask);

int total = counts[0] + counts[1];

Console.WriteLine(total);

static async Task<int> CalculateCountAsync(int count)
{
    await Task.Delay(100);
    return count;
}
```

Each task returns its own result. The top-level code combines the results after both operations finish, so neither operation needs to mutate shared state.

When shared mutation is required, use suitable synchronization or thread-safe data structures. However, do not introduce locks or concurrent collections without a demonstrated need. Clear ownership of data is often the simplest solution.

### 12. Asynchronous programming in a Boids simulation

A browser-based Boids simulation may need to load configuration, obtain initial state, or communicate with a server. These operations are often I/O-bound and are good candidates for asynchronous APIs.

The simulation's per-frame calculations are different. Updating hundreds or thousands of boids may involve intensive numerical work. That work is CPU-bound and may benefit from optimized algorithms or carefully designed parallel processing.

A useful separation is:

- Use `async` and `await` for asynchronous I/O.
- Use `Task.WhenAll` for independent asynchronous operations that should overlap.
- Use `Task.Run` selectively when CPU-bound work needs to run away from the calling thread.
- Use cancellation to stop work when it is no longer needed.
- Keep mutable simulation state under clear ownership so concurrent operations do not modify it unsafely.

In Blazor, asynchronous event handlers and lifecycle methods commonly use `Task`-returning methods. Awaiting asynchronous operations allows the framework to keep the interface responsive while work is pending. However, long-running synchronous calculations can still delay rendering and user interaction, so computational workloads need separate consideration.

The main lesson is that asynchronous programming is about managing operations that take time, especially when waiting. It is not a guarantee of parallel execution or faster calculations.

## Challenge

Build a console program that simulates loading Boids simulation data asynchronously and then performs a calculation using the results.

Your program must satisfy all of the following requirements:

1. **Create an asynchronous loading method:** Implement `LoadBoidCountAsync` that returns `Task<int>`. Use `Task.Delay` to simulate waiting, then return a positive boid count.
2. **Create a second asynchronous method:** Implement `LoadMovementSpeedAsync` that returns `Task<double>`. Simulate waiting and return a positive movement speed.
3. **Run operations concurrently:** Start both loading operations before awaiting them. Use `Task.WhenAll` or another clear approach to wait for both to complete. Do not await the first operation before starting the second.
4. **Display the results:** Print the loaded boid count and movement speed with clear labels.
5. **Create a CPU-bound calculation:** Implement a synchronous method that calculates a result from the loaded values. Keep it separate from the loading methods.
6. **Use `Task.Run` appropriately:** Run the calculation through `Task.Run` to demonstrate moving CPU-bound work to the thread pool. Explain in a comment that this is a demonstration and that trivial calculations do not normally need `Task.Run`.
7. **Support cancellation:** Accept a `CancellationToken` in the asynchronous loading methods and pass it to `Task.Delay`. Check cancellation where appropriate.
8. **Handle errors and cancellation:** Use `try`/`catch` to handle cancellation separately from ordinary operation failures. Do not silently swallow exceptions.
9. **Demonstrate sequential versus concurrent execution:** In a separate, clearly labeled demonstration, run two simulated loading operations sequentially and then concurrently. Use a `Stopwatch` to measure and print elapsed time for each approach. Use fresh operations for each measurement.
10. **Keep logic simple:** Use descriptive method names, return types, and variables. Keep the top-level statements focused on coordinating operations and displaying results.

The program should compile and run as a .NET console application. Use top-level `await` and avoid unnecessary classes, threads, or abstractions. The goal is to demonstrate task coordination, cancellation, and the distinction between I/O-bound waiting and CPU-bound work.
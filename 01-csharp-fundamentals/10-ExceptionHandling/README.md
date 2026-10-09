# Exception Handling

## Micro Goal

Understand how C# represents runtime errors with exceptions, how to handle expected failures using `try`, `catch`, and `finally`, and how to validate inputs and propagate errors without hiding bugs.

## Concepts Covered

- Exceptions and runtime failures
- `try`, `catch`, and `finally`
- Catching specific exception types
- Exception inheritance and catch order
- The `throw` statement
- Rethrowing exceptions with `throw;`
- Exception filters with `when`
- Input validation and guard clauses
- Common exceptions such as `ArgumentException`, `ArgumentOutOfRangeException`, `FormatException`, `OverflowException`, and `InvalidOperationException`
- `int.TryParse` and other non-exception-based validation methods
- Custom exception types
- Exception handling boundaries
- Error handling versus normal control flow
- Avoiding swallowed exceptions and overly broad catches

## Completion Criteria

- [ ] Explain what an exception represents and when it occurs.
- [ ] Use `try` and `catch` to handle an expected runtime failure.
- [ ] Catch specific exception types instead of indiscriminately catching every exception.
- [ ] Explain when a `finally` block runs and when it is appropriate to use one.
- [ ] Validate arguments with guard clauses and throw meaningful exceptions.
- [ ] Use `TryParse` to handle invalid user input without relying on exceptions.
- [ ] Explain the difference between `throw;` and `throw ex;`.
- [ ] Use an exception filter when handling depends on a condition.
- [ ] Define and throw a custom exception when a domain-specific error warrants one.
- [ ] Design a program that handles expected failures without concealing programming errors.

## Lecture

### 1. What is an exception?

An exception is an object that represents an error or an unusual condition during program execution. When an operation cannot proceed normally, .NET code can throw an exception to signal the failure.

For example, accessing an invalid array index throws an exception:

```csharp
int[] values = { 10, 20, 30 };

Console.WriteLine(values[10]);
```

The array has only three elements, so index `10` is invalid. The runtime throws an `IndexOutOfRangeException`. If the exception is not handled, it propagates out of the program's current execution path and can terminate the application.

Other common causes include parsing invalid text as a number, accessing a missing dictionary key, passing an invalid argument to a method, or attempting an operation that is not valid in the current state.

An exception is not automatically a bug. Some exceptions indicate programming errors, while others represent failures that an application may reasonably expect and handle. For example, invalid user input is expected in an interactive application. An index error caused by incorrect loop bounds is more likely a programming defect.

The objective of exception handling is not to eliminate every exception. It is to handle failures at the appropriate boundary while preserving enough information to diagnose unexpected problems.

### 2. The `try` and `catch` statements

A `try` block contains code that might throw an exception. A `catch` block handles a matching exception if one is thrown.

```csharp
try
{
    int[] values = { 10, 20, 30 };
    Console.WriteLine(values[10]);
}
catch (IndexOutOfRangeException)
{
    Console.WriteLine("The requested array index is invalid.");
}
```

If the exception occurs inside the `try` block, execution transfers to the matching `catch` block. The statement after the `try`/`catch` construct can then execute normally.

A `catch` block can also declare a variable to access the exception object:

```csharp
try
{
    int number = int.Parse("not a number");
    Console.WriteLine(number);
}
catch (FormatException exception)
{
    Console.WriteLine($"Invalid number format: {exception.Message}");
}
```

The `Message` property describes the failure. Other exception properties, such as `StackTrace` and `InnerException`, can help diagnose where a failure originated or whether another exception caused it.

Do not display internal exception details directly to end users in production applications when those details could expose implementation information. A user-facing message should explain what they can do next, while diagnostic details belong in an appropriate logging system.

### 3. Catch specific exception types

C# exceptions form an inheritance hierarchy. Most application exceptions derive from `Exception`, and more specific exception types describe particular kinds of failure.

For example:

- `FormatException`: text could not be interpreted in the expected format.
- `OverflowException`: a numeric operation or conversion exceeded its supported range.
- `ArgumentException`: an argument is invalid.
- `ArgumentOutOfRangeException`: an argument is outside an acceptable range.
- `InvalidOperationException`: an operation is invalid for the object's current state.
- `IndexOutOfRangeException`: an array index is outside the valid range.
- `KeyNotFoundException`: a dictionary lookup requested a key that does not exist.

Prefer catching the specific exception you know how to handle:

```csharp
try
{
    int count = int.Parse("invalid");
    Console.WriteLine(count);
}
catch (FormatException)
{
    Console.WriteLine("Enter a valid integer.");
}
```

A broad `catch (Exception)` can also catch this failure, but it does not communicate which failure you expected or how you intend to recover.

Catch a broader exception type only when the code genuinely has a recovery strategy for the broader category. Catching every exception and continuing can hide bugs, leave the program in an invalid state, or make failures difficult to diagnose.

If you need multiple `catch` blocks, order them from the most specific exception type to the most general matching type. A broad handler placed before a narrower handler can make the narrower handler unreachable.

### 4. The `finally` block

A `finally` block runs when control leaves its associated `try`/`catch` construct, whether execution completes normally or an exception is thrown and handled or propagated.

```csharp
try
{
    Console.WriteLine("Starting operation.");
    throw new InvalidOperationException("Operation failed.");
}
catch (InvalidOperationException)
{
    Console.WriteLine("Handled the operation failure.");
}
finally
{
    Console.WriteLine("Cleanup runs here.");
}
```

The output is:

```text
Starting operation.
Handled the operation failure.
Cleanup runs here.
```

The `finally` block is intended for cleanup that must occur regardless of success or failure. Historically, it is often used to release resources or restore state.

For many .NET resources, `using` or `using` declarations are preferable because they dispose of objects implementing `IDisposable` automatically:

```csharp
using StreamReader reader = File.OpenText("data.txt");
string contents = reader.ReadToEnd();
```

The compiler arranges disposal even if an exception occurs while reading. You do not need to write a manual `finally` block just to dispose of this reader.

A `finally` block is not guaranteed to run if the process terminates abruptly, such as through a power failure or forced termination. Do not use it as a guarantee that important data will always be saved.

### 5. Throwing exceptions with `throw`

Your own methods can throw exceptions when callers supply invalid arguments or request operations that cannot be performed.

Consider a method that sets a boid's movement speed:

```csharp
static double ValidateSpeed(double speed)
{
    if (speed < 0)
    {
        throw new ArgumentOutOfRangeException(
            nameof(speed),
            "Speed cannot be negative.");
    }

    return speed;
}
```

The method rejects a negative speed rather than silently accepting invalid state.

A guard clause checks a precondition early and exits by throwing an exception when the condition is not satisfied. This keeps the main logic simple because invalid inputs are handled before the operation proceeds.

For numeric values, checking only `speed < 0` is not always enough. Floating-point values can also be `NaN` or infinity. If a method requires a finite, non-negative speed, validate both conditions:

```csharp
static double ValidateSpeed(double speed)
{
    if (!double.IsFinite(speed) || speed < 0)
    {
        throw new ArgumentOutOfRangeException(
            nameof(speed),
            "Speed must be finite and non-negative.");
    }

    return speed;
}
```

Use the exception type that best describes the contract violation. `ArgumentNullException` is appropriate for an unexpected null argument, `ArgumentOutOfRangeException` for a value outside the allowed range, and `ArgumentException` for other invalid argument values.

Do not use exceptions as a substitute for ordinary branching when an outcome is expected and easy to check directly.

### 6. `throw;` versus `throw ex;`

Sometimes a method catches an exception only to perform a local action, such as recording diagnostic information, before allowing the failure to continue.

When rethrowing an exception from a `catch` block, use `throw;`:

```csharp
try
{
    PerformSimulationStep();
}
catch (InvalidOperationException)
{
    Console.WriteLine("The simulation step failed.");
    throw;
}
```

The `throw;` statement rethrows the currently handled exception while preserving its original stack trace.

By contrast, writing `throw exception;` where `exception` is the caught exception resets the stack-trace origin to the rethrow location, making the original failure location harder to identify.

Avoid catching and rethrowing exceptions when the catch block adds no useful behavior. If you have no recovery, cleanup, or meaningful diagnostic action to perform, allowing the exception to propagate naturally is usually simpler.

### 7. Exception filters with `when`

An exception filter lets you handle an exception only when an additional condition is satisfied.

For example, a program might treat a particular invalid-operation failure differently depending on whether the simulation is running:

```csharp
bool isSimulationRunning = false;

try
{
    if (!isSimulationRunning)
    {
        throw new InvalidOperationException(
            "The simulation is not running.");
    }
}
catch (InvalidOperationException exception)
    when (!isSimulationRunning)
{
    Console.WriteLine(exception.Message);
}
```

The `when` condition is evaluated when deciding whether the handler matches. If the filter is false, that `catch` block is skipped, and the exception continues searching for another matching handler.

Use exception filters when the exception type alone is not enough to determine whether a handler should apply. Do not add filters that merely repeat the exception type's meaning or make the code harder to follow.

### 8. Prefer `TryParse` for expected input errors

If a user enters text that may or may not represent an integer, invalid input is a normal possibility. You generally should not use exceptions to handle this routine condition.

Instead of this:

```csharp
try
{
    int boidCount = int.Parse("many");
    Console.WriteLine(boidCount);
}
catch (FormatException)
{
    Console.WriteLine("Invalid count.");
}
```

Use `int.TryParse`:

```csharp
string input = "many";

if (int.TryParse(input, out int boidCount))
{
    Console.WriteLine($"Boid count: {boidCount}");
}
else
{
    Console.WriteLine("Enter a valid integer.");
}
```

`TryParse` returns `true` if the conversion succeeds and `false` if it fails. The `out` parameter receives the parsed value on success.

The same pattern exists for other numeric types, including `double.TryParse` and `decimal.TryParse`.

Parsing and validation are separate concerns. The string `"0"` parses successfully as an integer, but zero might not be an acceptable count for a particular operation. Validate the value after parsing:

```csharp
if (!int.TryParse(input, out int boidCount))
{
    Console.WriteLine("Enter a valid integer.");
}
else if (boidCount <= 0)
{
    Console.WriteLine("The boid count must be positive.");
}
else
{
    Console.WriteLine($"Creating {boidCount} boids.");
}
```

Use `TryParse` for expected conversion failures and guard clauses for invalid values that violate a method's contract.

### 9. Custom exception types

The built-in exception types cover many common failure conditions. A custom exception can be useful when an application has a meaningful domain-specific failure that callers need to distinguish from other errors.

For example, a simulation might define an exception for a requested operation that cannot be completed because the simulation configuration is invalid:

```csharp
class InvalidSimulationConfigurationException : Exception
{
    public InvalidSimulationConfigurationException(string message)
        : base(message)
    {
    }
}
```

A method could then throw it when a configuration-specific rule is violated:

```csharp
static void ValidateBoidCount(int count)
{
    if (count <= 0)
    {
        throw new InvalidSimulationConfigurationException(
            "The simulation must contain at least one boid.");
    }
}
```

Callers can catch the custom type specifically if they have a meaningful way to respond:

```csharp
try
{
    ValidateBoidCount(0);
}
catch (InvalidSimulationConfigurationException exception)
{
    Console.WriteLine(exception.Message);
}
```

A custom exception should represent a distinct failure category that callers have a reason to recognize. Do not create a new exception type for every validation condition when `ArgumentException` or `ArgumentOutOfRangeException` already communicates the problem adequately.

### 10. Exception handling boundaries

Exception handling is most useful where the program can make a meaningful decision about recovery.

For example, a method that calculates a simulation update should normally not catch every exception internally and pretend the update succeeded. Doing so could leave simulation state partially updated.

Instead, validate inputs near the method boundary, let unexpected failures propagate, and catch exceptions at a layer that can decide what happens next.

A console application might catch an expected input failure and ask the user to retry. A web application might translate a known validation failure into an appropriate response. A background worker might log an unexpected failure and stop or retry according to an explicit policy.

Avoid this pattern:

```csharp
try
{
    UpdateSimulation();
}
catch (Exception)
{
    // Do nothing.
}
```

This silently discards every failure, including defects that should be investigated. The application may continue with inconsistent state and provide no useful diagnostic information.

A better design distinguishes between expected invalid input, recoverable operational failures, and unexpected programming errors.

### 11. Common mistakes

- **Catching too broadly:** Handling all exceptions without a recovery strategy can hide defects.
- **Swallowing exceptions:** An empty `catch` block loses important failure information.
- **Using exceptions for normal branching:** Prefer `TryParse`, conditionals, and ordinary return values when failure is expected.
- **Ignoring validation:** Successfully parsing a value does not guarantee that it is within the valid range.
- **Rethrowing incorrectly:** Use `throw;` rather than `throw exception;` when rethrowing the currently handled exception.
- **Assuming `finally` always runs:** Abrupt process termination can prevent cleanup code from executing.
- **Creating excessive custom exceptions:** Use built-in exception types unless a domain-specific category has practical value.
- **Continuing after an unsafe failure:** Do not catch an exception and continue if the program's state may no longer be valid.

Good exception handling makes failure behavior explicit. It protects method contracts, handles expected problems appropriately, and preserves unexpected failures for diagnosis rather than concealing them.

## Challenge

Build a console program that validates and executes a simple Boids simulation configuration. The program should demonstrate input validation, expected parsing failures, exception handling, and safe propagation of unexpected failures.

Your program must satisfy all of the following requirements:

1. **Validate a boid count:** Implement a method that rejects a count less than or equal to zero using `ArgumentOutOfRangeException`.
2. **Validate movement speed:** Implement a method that rejects negative, `NaN`, or infinite speed values using an appropriate exception type.
3. **Parse user input safely:** Use `int.TryParse` to read or simulate a boid-count input. Handle invalid text without relying on exceptions for ordinary parsing failure.
4. **Handle a specific exception:** Use `try` and `catch` to handle a known validation failure and print a useful message.
5. **Use `finally`:** Demonstrate that a cleanup or completion message runs whether an operation succeeds or a handled exception occurs.
6. **Rethrow correctly:** Demonstrate `throw;` in a catch block that performs a meaningful action before allowing the original exception to propagate. Ensure the rethrown exception is ultimately handled by an appropriate caller or is deliberately allowed to terminate the program.
7. **Use an exception filter:** Demonstrate a `catch` filter with `when` where the condition meaningfully determines whether the handler applies.
8. **Keep exception handling appropriate:** Do not catch all exceptions merely to continue execution. Do not use exceptions for ordinary invalid text when `TryParse` is sufficient.
9. **Keep logic focused:** Put validation in separate methods and keep the top-level statements focused on invoking operations and displaying results.

Use deterministic inputs for the demonstration so the program's output is reproducible. Keep the implementation concise and ensure each exception-handling construct demonstrates a distinct purpose.
# Generics and Delegates

## Micro Goal

Understand how generics allow reusable, type-safe code and how delegates allow methods to be passed around as values, providing the foundation for callbacks, event handling, and flexible behavior in C#.

## Concepts Covered

- Generic types and methods
- Type parameters and type arguments
- Generic constraints with `where`
- Generic classes and reusable data structures
- Generic methods and type inference
- The `default` value for generic types
- Delegates and method references
- Built-in delegate types: `Action<T>`, `Func<T, TResult>`, and `Predicate<T>`
- Lambda expressions
- Anonymous functions
- Multicast delegates
- Events and the publisher-subscriber pattern
- Delegates as callbacks
- Generic collections and type safety
- Applying generics and delegates to simulation logic

## Completion Criteria

- [ ] Explain the difference between a generic type parameter and a concrete type argument.
- [ ] Create and use a generic class.
- [ ] Write a generic method that works with more than one data type.
- [ ] Use a generic constraint to restrict valid type arguments.
- [ ] Explain why generics provide compile-time type safety.
- [ ] Declare a delegate and assign a compatible method to it.
- [ ] Use `Action<T>`, `Func<T, TResult>`, and `Predicate<T>`.
- [ ] Write lambda expressions that match delegate signatures.
- [ ] Pass a delegate into a method to customize its behavior.
- [ ] Explain multicast delegates and how events use delegates.
- [ ] Build a small simulation-related example that combines generic code with delegates.

## Lecture

### 1. Why generics exist

Generics let you write classes, methods, and interfaces that work with different types while retaining compile-time type safety.

For example, a simulation might need to store boids, obstacles, or numeric values. You could write separate collection implementations for each type, but that would duplicate code.

C# already provides generic collections such as `List<T>`:

```csharp
List<int> counts = new();
counts.Add(10);
counts.Add(20);

List<string> names = new();
names.Add("Boid Alpha");
names.Add("Boid Beta");
```

The `T` in `List<T>` is a type parameter. When you write `List<int>`, `int` is the type argument. When you write `List<string>`, `string` is the type argument.

Each list is restricted to its declared element type. You cannot add a string to `List<int>`:

```csharp
List<int> counts = new();
counts.Add(10);

// This does not compile:
// counts.Add("ten");
```

The compiler catches the type mismatch before the program runs. This is one of the main advantages of generics over collections that store arbitrary objects and require casts when retrieving values.

### 2. Writing a generic class

You can define your own generic class by placing type parameters in angle brackets after the class name.

```csharp
class Storage<T>
{
    public T Value { get; set; }

    public Storage(T value)
    {
        Value = value;
    }
}
```

The class does not specify a concrete type for `Value`. It uses the type parameter `T`, which is supplied when the class is constructed.

```csharp
Storage<int> countStorage = new(100);
Storage<string> nameStorage = new("Boids");

Console.WriteLine(countStorage.Value);
Console.WriteLine(nameStorage.Value);
```

The first object stores an integer, and the second stores a string. Both use the same generic class definition.

Generics are especially useful when the behavior of a class is independent of the specific type of data it stores or processes.

For example, a reusable container should not need separate implementations for `int`, `double`, and `string`. A generic implementation can handle all three while preserving their types.

### 3. Writing a generic method

Methods can also declare type parameters. A generic method can accept or return values of a type that is determined when the method is called.

```csharp
static T GetFirst<T>(T first, T second)
{
    return first;
}
```

You can call the method with different types:

```csharp
int firstCount = GetFirst<int>(10, 20);
string firstName = GetFirst<string>("Alpha", "Beta");
```

C# can often infer the type argument from the supplied arguments, so the explicit type argument is unnecessary:

```csharp
int firstCount = GetFirst(10, 20);
string firstName = GetFirst("Alpha", "Beta");
```

The compiler infers `T` as `int` in the first call and `string` in the second.

Generic methods are useful when an operation is the same regardless of the type being processed. For example, a method that returns the first item of a pair does not need to know whether the values are integers, strings, or boid objects.

### 4. Generic methods with collections

A common use for generics is writing reusable methods that operate on collections.

```csharp
static void PrintItems<T>(IEnumerable<T> items)
{
    foreach (T item in items)
    {
        Console.WriteLine(item);
    }
}
```

This method accepts any sequence of values of type `T`. `IEnumerable<T>` is a generic interface representing something that can be iterated over.

You can pass lists of different types:

```csharp
List<int> counts = new() { 10, 20, 30 };
List<string> names = new() { "Alpha", "Beta" };

PrintItems(counts);
PrintItems(names);
```

The method does not depend on `List<T>` specifically. It depends on the more general `IEnumerable<T>` contract, so arrays and many other collection types can also be passed.

This illustrates an important design principle: use the least restrictive type that provides the operations your method actually needs.

### 5. Generic constraints

Sometimes a generic method or class requires its type parameter to support certain operations or have certain characteristics. A `where` clause applies a constraint to the type parameter.

For example:

```csharp
static T CreateInstance<T>() where T : new()
{
    return new T();
}
```

The `new()` constraint requires `T` to have a public parameterless constructor. This allows the method to construct an instance with `new T()`.

The constraint prevents the method from being used with types that do not meet that requirement.

Other common constraints include:

```csharp
where T : class
where T : struct
where T : notnull
where T : SomeBaseClass
where T : ISomeInterface
```

These constraints mean, respectively, that `T` must be a reference type, a non-nullable value type, a non-nullable type, a type derived from a specified base class, or a type implementing a specified interface.

For example, suppose the simulation needs a generic method that works only with simulation entities:

```csharp
static void UpdateAll<T>(IEnumerable<T> entities, double deltaTime)
    where T : SimulationEntity
{
    foreach (T entity in entities)
    {
        entity.Update(deltaTime);
    }
}
```

This method requires every `T` to derive from `SimulationEntity`. That constraint allows the method to call `Update` without checking the runtime type of each entity.

Constraints should reflect actual requirements. Avoid adding them when the method does not need them.

### 6. The `default` value for generic types

A generic type parameter can represent a value type or a reference type. The `default` expression provides the default value for the type being used.

```csharp
static T GetDefault<T>()
{
    return default!;
}
```

The null-forgiving operator in this example suppresses a nullable warning; it does not change the runtime value. In nullable-aware code, a more accurate signature for an unconstrained type parameter is often `T?`, depending on the intended behavior.

For common types, the default values include:

- `int`: `0`
- `double`: `0.0`
- `bool`: `false`
- Reference types: `null`

For example:

```csharp
int defaultCount = default;
double defaultSpeed = default;
bool defaultRunning = default;
string? defaultName = default;
```

The default value is useful when implementing generic containers or algorithms that need a type-appropriate initial value. However, it does not necessarily represent a valid application-level value. A default speed of zero may be valid, while a missing identifier represented by `null` may need special handling.

### 7. What delegates are

A delegate is a type-safe reference to a method. It allows a method to be stored in a variable, passed as an argument, or invoked later.

You can declare a delegate type explicitly:

```csharp
delegate double SpeedCalculator(double baseSpeed);
```

This delegate can reference methods that accept one `double` and return a `double`.

For example:

```csharp
static double DoubleSpeed(double baseSpeed)
{
    return baseSpeed * 2.0;
}
```

You can assign the method to a delegate variable and invoke it:

```csharp
SpeedCalculator calculator = DoubleSpeed;

double result = calculator(2.5);

Console.WriteLine(result); // 5
```

The delegate's signature must match the method's parameter types and return type. A method with an incompatible signature cannot be assigned to that delegate.

Delegates are useful when the code performing an operation should not need to know exactly which method will perform it.

For example, a simulation might calculate movement using one rule during normal operation and another rule during testing. A delegate can represent the calculation that should be used.

### 8. Built-in delegate types: `Action`, `Func`, and `Predicate`

C# provides common generic delegate types so you do not need to declare a new delegate type for every method signature.

**`Action`** represents a method that returns no value. Generic forms accept input parameters.

```csharp
Action<string> printMessage = message =>
{
    Console.WriteLine(message);
};

printMessage("Simulation started.");
```

`Action<string>` represents a method that accepts a string and returns `void`.

**`Func`** represents a method that returns a value. Its final type argument is the return type.

```csharp
Func<double, double> doubleSpeed = speed => speed * 2.0;

double result = doubleSpeed(3.0);

Console.WriteLine(result); // 6
```

`Func<double, double>` accepts one `double` and returns a `double`.

With multiple inputs, the final generic argument remains the return type:

```csharp
Func<double, double, double> calculateDistance =
    (x, y) => Math.Sqrt(x * x + y * y);
```

This delegate accepts two `double` values and returns a `double`.

**`Predicate<T>`** represents a method that accepts a value of type `T` and returns a `bool`.

```csharp
Predicate<int> isPositive = count => count > 0;

Console.WriteLine(isPositive(10));  // True
Console.WriteLine(isPositive(-10)); // False
```

These types cover many common callback needs:

- Use `Action<T>` when an operation receives input but returns no result.
- Use `Func<T, TResult>` when an operation produces a result.
- Use `Predicate<T>` when an operation answers a yes-or-no question about a value.

### 9. Lambda expressions

A lambda expression is a concise way to define an anonymous function. It is often used to create a delegate without declaring a separate named method.

The basic form is:

```csharp
(parameters) => expression
```

For example:

```csharp
Func<double, double> increaseSpeed = speed => speed + 0.5;
```

The expression after `=>` is evaluated and returned because the lambda contains a single expression.

For multiple statements, use a block:

```csharp
Func<double, double> normalizeSpeed = speed =>
{
    if (speed < 0)
    {
        return 0.0;
    }

    return speed;
};
```

The lambda can be invoked like any other delegate:

```csharp
Console.WriteLine(increaseSpeed(2.0));  // 2.5
Console.WriteLine(normalizeSpeed(-1.0)); // 0
```

A lambda expression is particularly useful when the behavior is short and only needed at one call site. If the behavior is complex or reused widely, a named method can be clearer.

### 10. Passing delegates as parameters

A method can accept a delegate to let its caller customize part of its behavior.

```csharp
static double CalculateUpdatedSpeed(
    double currentSpeed,
    Func<double, double> adjustment)
{
    return adjustment(currentSpeed);
}
```

The method accepts a speed and a function that transforms it.

You can supply different behavior without changing `CalculateUpdatedSpeed`:

```csharp
double accelerated = CalculateUpdatedSpeed(
    2.0,
    speed => speed + 0.5);

double doubled = CalculateUpdatedSpeed(
    2.0,
    speed => speed * 2.0);
```

The first call adds `0.5`; the second doubles the value.

This pattern is useful for callbacks, configurable algorithms, and reusable operations. The receiving method defines when the supplied function is invoked, while the caller determines what behavior it represents.

Delegates can also refer to named methods:

```csharp
static double Accelerate(double speed)
{
    return speed + 0.5;
}

double updatedSpeed = CalculateUpdatedSpeed(2.0, Accelerate);
```

Named methods can improve readability when behavior is substantial or used in multiple places.

### 11. Delegates and collection operations

Delegates frequently appear in collection APIs. For example, `List<T>.Find` accepts a predicate that identifies a matching element.

```csharp
List<int> counts = new() { 0, 10, 25, 50 };

int firstPositive = counts.Find(count => count > 0);

Console.WriteLine(firstPositive); // 10
```

The lambda `count => count > 0` becomes a predicate that returns `true` for positive counts.

You can use similar predicates with `FindAll`:

```csharp
List<int> largeCounts = counts.FindAll(count => count >= 25);
```

The returned list contains values that satisfy the condition.

This is another example of separating the operation from the rule that controls it. The collection method performs the search or filtering; the delegate supplies the condition.

### 12. Multicast delegates

A delegate can reference more than one compatible method. This is called a multicast delegate.

```csharp
Action<string> logMessage = message => Console.WriteLine($"Console: {message}");

logMessage += message => Console.WriteLine($"Audit: {message}");

logMessage("Simulation started.");
```

Both registered methods execute when the delegate is invoked.

Multicast delegates are useful when one occurrence should notify multiple subscribers. However, multicast delegates have important behavior to understand: for delegates returning a value, invoking the multicast delegate returns the result from the last method in the invocation list. Earlier return values are not collected automatically.

If an individual method throws an exception, normal invocation stops at that exception unless the caller explicitly handles subscriber invocations separately.

For notification systems, C# events provide a more controlled way to expose multicast delegates.

### 13. Events and the publisher-subscriber pattern

An event allows an object to notify other objects when something happens without needing to know the details of each subscriber.

The object that raises an event is the **publisher**. Objects that subscribe to the event are **subscribers**.

For example, a simulation might publish an event when an update completes:

```csharp
class Simulation
{
    public event Action<double>? UpdateCompleted;

    public void CompleteUpdate(double deltaTime)
    {
        UpdateCompleted?.Invoke(deltaTime);
    }
}
```

The event uses `Action<double>`, meaning subscribers receive the elapsed time and return no value.

Subscribe with `+=`:

```csharp
Simulation simulation = new();

simulation.UpdateCompleted += deltaTime =>
{
    Console.WriteLine($"Update completed: {deltaTime:F2}");
};

simulation.CompleteUpdate(0.016);
```

The `?.Invoke` expression invokes the event only if it has subscribers. Without subscribers, the event is `null`, so no invocation occurs.

Outside the class that declares the event, code can subscribe and unsubscribe, but it cannot directly invoke the event. This protects the publisher's control over when notifications occur.

Unsubscribe with `-=` when a subscriber no longer needs notifications:

```csharp
Action<double> handler = deltaTime =>
    Console.WriteLine($"Update: {deltaTime}");

simulation.UpdateCompleted += handler;
simulation.UpdateCompleted -= handler;
```

Retaining the handler reference makes it possible to remove the same delegate later. This matters for long-lived publishers, where forgotten subscriptions can keep subscriber objects alive or cause unwanted callbacks.

Events are useful for notifications such as simulation completion, state changes, user actions, or background work finishing. They should not replace direct method calls when the caller already knows exactly which operation it needs to perform.

### 14. Combining generics and delegates

Generics and delegates solve complementary problems.

- Generics let code operate on different types while retaining type safety.
- Delegates let code accept different behaviors while retaining a known method signature.

For example, a generic method can find the first item that matches a supplied predicate:

```csharp
static T? FindFirst<T>(
    IEnumerable<T> items,
    Predicate<T> matches)
{
    foreach (T item in items)
    {
        if (matches(item))
        {
            return item;
        }
    }

    return default;
}
```

The method can work with any element type and any matching rule compatible with `Predicate<T>`.

Example usage:

```csharp
List<int> counts = new() { 0, 10, 25, 50 };

int? firstLargeCount = FindFirst(
    counts,
    count => count >= 25);
```

The method is generic because it accepts any `T`. The predicate is a delegate because the caller supplies the matching behavior.

There is one design detail to handle in production code: `default` can be a legitimate value for a value type, so it may not clearly indicate whether a match was found. An alternative is to return a Boolean and use an `out` parameter, or return a nullable result where appropriate. The best choice depends on the types and how absence should be represented.

Generics and delegates are foundational tools throughout .NET. They appear in collection APIs, LINQ, event systems, asynchronous workflows, and many reusable libraries.

## Challenge

Build a small console program that manages generic simulation data and uses delegates to customize how that data is processed.

Your program must satisfy all of the following requirements:

1. **Create a generic container:** Define `SimulationBuffer<T>` that stores a collection of values of type `T`. Include methods to add an item, retrieve the item count, and expose the stored items for iteration without allowing callers to replace the internal collection.
2. **Use the generic container:** Create one buffer of `double` values representing boid speeds and another buffer of `string` values representing boid identifiers. Add multiple items to each and print their contents.
3. **Create a generic method:** Implement `PrintItems<T>` that accepts an `IEnumerable<T>` and prints every item. Use it with both buffers.
4. **Use a generic constraint:** Implement a method constrained to `SimulationEntity` or one of its derived types. Have it call `Update(deltaTime)` for each entity without using runtime type checks.
5. **Declare a delegate:** Define a `SpeedAdjustment` delegate that accepts a speed and returns an adjusted speed.
6. **Use multiple implementations:** Provide at least two speed-adjustment methods, such as acceleration and speed scaling, that match the delegate signature.
7. **Pass a delegate as an argument:** Implement a method that applies a supplied `SpeedAdjustment` to a speed. Call it with both named methods.
8. **Use a lambda:** Pass a lambda expression to a method or collection operation to select or filter values, such as finding speeds above a threshold.
9. **Demonstrate an event:** Create a simple publisher that raises an event when a simulation update completes. Subscribe a handler, trigger the event, and print the notification.
10. **Keep responsibilities clear:** Place the generic buffer, delegate-based calculations, and event publisher in appropriately named types or methods. Keep the top-level statements focused on creating data, calling operations, and displaying results.

Use explicit types where they improve clarity, and keep the implementation straightforward. Avoid adding a framework or unrelated abstractions. The goal is to demonstrate how generic type parameters and delegates make code reusable without sacrificing type safety or readability.
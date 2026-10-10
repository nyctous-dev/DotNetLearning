# Methods

## Micro Goal

Understand how to define and call methods in C#, pass data through parameters, return results, and organize code into small, reusable units of behavior.

## Concepts Covered

- **Method declaration and invocation** — defining and calling methods.
- **Parameters and arguments** — passing data into methods.
- **Return types** — returning results with `return` and using `void` for methods that return no value.
- **Value and reference types** — understanding how arguments are passed to methods.
- **Method overloading** — defining methods with the same name but different parameter lists.
- **Optional and named arguments** — making method calls more flexible and readable.
- **Local scope** — understanding where variables can be accessed.
- **Expression-bodied methods** — writing concise methods for simple expressions.

## Completion Criteria

Be able to define and call methods, select appropriate parameter and return types, explain how arguments are passed, use method overloading and named arguments, and divide a program into small methods with clear responsibilities.

## Lecture

### 1. What Is a Method?

A method is a named block of code that performs a specific operation. Methods allow you to reuse behavior instead of repeatedly writing the same statements.

Without a method:

```
Console.WriteLine("Simulation started.");
Console.WriteLine("Simulation started.");
```

With a method:

```
static void PrintStartMessage()
{
    Console.WriteLine("Simulation started.");
}

PrintStartMessage();
PrintStartMessage();
```

The method is declared once and called twice. Each call executes its body.

Methods make programs easier to read, test, debug, and maintain. In our Boids simulation, we might have methods such as `CalculateDistance`, `UpdateVelocity`, and `PrintSimulationStatus`.

### 2. Declaring and Calling Methods

A typical method declaration looks like this:

```
static returnType MethodName(parameters)
{
    // Method body
}
```

The parts are:

- `static` — makes the method belong to the type rather than an instance of that type.
- `returnType` — specifies the kind of value returned, or `void` if no value is returned.
- `MethodName` — the name used to call the method.
- `parameters` — the inputs the method accepts.
- The method body — the statements executed when the method is called.

In a console project using top-level statements, you can declare local functions after the executable statements:

```
PrintGreeting();

static void PrintGreeting()
{
    Console.WriteLine("Hello, Boids!");
}
```

This is a convenient way to practice methods without introducing classes yet. We'll cover classes and object-oriented design in later modules.

### 3. Parameters and Arguments

Parameters define the inputs a method accepts. Arguments are the actual values supplied when calling it.

```
PrintBoidCount(100);

static void PrintBoidCount(int count)
{
    Console.WriteLine($"Boid count: {count}");
}
```

Here, `count` is the parameter and `100` is the argument.

A method can accept multiple parameters:

```
PrintPosition(10.5, 20.0);

static void PrintPosition(double x, double y)
{
    Console.WriteLine($"Position: ({x}, {y})");
}
```

The arguments must be compatible with the corresponding parameter types.

Parameters let methods work with different data without hardcoding specific values into their implementation.

### 4. Return Types and `return`

A method can return a value to the code that called it.

```
int Add(int first, int second)
{
    return first + second;
}
```

In a top-level program, declare the local function as `static` when it doesn't need access to local variables:

```
int result = Add(10, 20);
Console.WriteLine(result); // 30

static int Add(int first, int second)
{
    return first + second;
}
```

The return type `int` means the method must return an integer. The `return` statement sends the result back to the caller and ends that method invocation.

A `void` method performs an operation without returning a value:

```
static void PrintMessage()
{
    Console.WriteLine("Running simulation.");
}
```

A non-void method must return a compatible value on every reachable path that requires a return. A `void` method may use `return;` to exit early.

### 5. Methods and Expressions

A method can return the result of an expression directly:

```
static double CalculateSpeed(double baseSpeed, double multiplier)
{
    return baseSpeed * multiplier;
}
```

For simple methods, an expression-bodied form may be clearer:

```
static double CalculateSpeed(double baseSpeed, double multiplier)
    => baseSpeed * multiplier;
```

The `=>` syntax means the expression's result is returned. Use expression-bodied methods when they improve readability, not merely to reduce line count.

### 6. Value and Reference Types in Method Calls

By default, C# passes arguments by value. The method receives a copy of the argument's value.

For a value type such as `int`, changing the parameter does not change the caller's variable:

```
int count = 100;
IncreaseCount(count);

Console.WriteLine(count); // 100

static void IncreaseCount(int value)
{
    value += 1;
}
```

The method changes only its local copy.

For a reference type, the copied value is a reference to an object. The method can use that reference to modify the object's state, although reassigning the parameter does not reassign the caller's variable.

```
var boidCounts = new List<int> { 10, 20 };

AddBoidCount(boidCounts);

Console.WriteLine(boidCounts.Count); // 3

static void AddBoidCount(List<int> counts)
{
    counts.Add(30);
}
```

The list is modified because both references point to the same list object.

The important distinction is that **C# still passes the reference by value**. The method receives a copy of the reference, not an independent copy of the object.

You can use `ref` or `out` for specialized cases where a method needs to work with a caller's variable directly. These are useful language features, but ordinary return values are often clearer when a method needs to produce a result.

### 7. Method Overloading

Method overloading allows multiple methods to share a name while accepting different parameter lists.

```
static double CalculateSpeed(double baseSpeed, double multiplier)
{
    return baseSpeed * multiplier;
}

static double CalculateSpeed(double baseSpeed, double multiplier, double boost)
{
    return baseSpeed * multiplier * boost;
}
```

The compiler selects the appropriate overload based on the arguments supplied:

```
Console.WriteLine(CalculateSpeed(2.0, 1.5));       // 3.0
Console.WriteLine(CalculateSpeed(2.0, 1.5, 2.0)); // 6.0
```

Overloads must differ in their parameter signatures. You cannot overload a method solely by changing its return type.

Use overloading when several versions of an operation represent the same conceptual behavior with different inputs.

### 8. Optional and Named Arguments

Optional parameters provide default values when callers omit an argument.

```
static void PrintSimulationStatus(string name, bool isRunning = true)
{
    Console.WriteLine($"{name}: Running = {isRunning}");
}
```

You can call the method with or without the optional argument:

```
PrintSimulationStatus("Boids");
PrintSimulationStatus("Boids", false);
```

Named arguments specify which parameter receives a value:

```
PrintSimulationStatus(name: "Boids", isRunning: false);
```

Named arguments improve readability when a method accepts several parameters of similar types or when a Boolean argument would otherwise be unclear.

Optional parameters should have sensible defaults. Avoid using them when the omitted value would hide important behavior or make the method ambiguous.

### 9. Local Scope

Scope determines where a variable or local function can be accessed.

A variable declared inside a method or block is local to that scope:

```
static void PrintCount()
{
    int count = 100;
    Console.WriteLine(count);
}

// count is not accessible here.
```

A local variable cannot be used outside the block in which it is declared. This helps keep data isolated and prevents unrelated code from depending on implementation details.

Method parameters are also local to the method. Different methods can use the same parameter name without conflicting with each other.

Prefer keeping variables within the smallest scope that makes sense. This reduces accidental dependencies and makes code easier to understand.

### 10. Designing Good Methods

A good method has a clear purpose, a meaningful name, and a well-defined contract: what inputs it accepts, what result it returns, and what effects it may have.

For example:

```
static double CalculateDistance(double x1, double y1, double x2, double y2)
{
    double deltaX = x2 - x1;
    double deltaY = y2 - y1;

    return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
}
```

This method calculates the Euclidean distance between two points. It accepts four coordinates and returns one result.

A method like `CalculateDistance` is easier to reuse and test than a long block of calculations embedded inside unrelated logic.

As a general guideline:

- Give methods names that describe their behavior.
- Keep each method focused on one responsibility.
- Pass required inputs explicitly.
- Return calculated results rather than relying on unnecessary shared state.
- Avoid duplicating the same logic across multiple places.
- Prefer clarity over clever or overly compressed code.

## Challenge

Create a console program that uses methods to perform basic calculations for a Boids simulation.

Your program must:

1. Define a method that calculates the distance between two points using their X and Y coordinates.
2. Define a method that calculates movement speed from a base speed and a multiplier.
3. Define a `void` method that prints a simulation status message using a parameter.
4. Define overloaded methods for calculating speed, with one overload accepting an additional boost multiplier.
5. Use an optional parameter in a method to provide a sensible default value.
6. Use a named argument in at least one method call.
7. Demonstrate that changing a value-type parameter inside a method does not change the caller's variable.
8. Call each method and print its results clearly.

Use appropriate parameter and return types, descriptive names, and small methods with clear responsibilities. Avoid placing all calculations directly in the top-level statements.

Implement the solution independently in `Program.cs`. When it runs, share your code for review against the completion criteria before moving on.

Add method naming and accessibility guidanceClarify local functions versus class methods
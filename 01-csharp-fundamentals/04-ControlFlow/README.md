# Control Flow

## Micro Goal

Understand how to control the order in which C# code executes using conditional statements, loops, and branching.

## Concepts Covered

- **Conditional statements** — `if`, `else if`, and `else`.
- **Switch statements and expressions** — selecting behavior based on a value.
- **For loops** — repeating code a known number of times.
- **While and do-while loops** — repeating code while a condition holds.
- **Foreach loops** — iterating through collections.
- **Break and continue** — controlling loop execution.
- **Nested control flow** — combining conditions and loops.

## Completion Criteria

Be able to select an appropriate control-flow construct, write conditions that determine program behavior, iterate over data, and control when loops continue or terminate.

## Lecture

### 1. What Is Control Flow?

Control flow determines which statements execute, in what order, and how often.

By default, C# executes statements sequentially from top to bottom. Control-flow statements let a program make decisions and repeat work.

```csharp
Console.WriteLine("First");
Console.WriteLine("Second");
Console.WriteLine("Third");
```

These statements execute in order. Conditional statements and loops let us change that behavior based on data.

### 2. Conditional Statements

An `if` statement executes a block only when its condition evaluates to `true`.

```csharp
int boidCount = 100;

if (boidCount > 0)
{
    Console.WriteLine("The simulation has boids.");
}
```

The condition must evaluate to a Boolean value.

Use `else` to provide an alternative when the condition is false:

```csharp
if (boidCount > 0)
{
    Console.WriteLine("Boids are present.");
}
else
{
    Console.WriteLine("The simulation is empty.");
}
```

Use `else if` when there are multiple mutually exclusive conditions to evaluate:

```csharp
if (boidCount == 0)
{
    Console.WriteLine("Empty simulation.");
}
else if (boidCount < 100)
{
    Console.WriteLine("Small simulation.");
}
else
{
    Console.WriteLine("Large simulation.");
}
```

Conditions are checked in order. Once a branch executes, the remaining branches in that chain are skipped.

Use braces `{ }` for conditional blocks. They make the scope of each branch explicit and help prevent mistakes when code changes.

### 3. Switch Statements and Expressions

A `switch` is useful when selecting behavior based on a particular value.

A switch statement:

```csharp
char displayMode = 'A';

switch (displayMode)
{
    case 'A':
        Console.WriteLine("Displaying all boids.");
        break;

    case 'S':
        Console.WriteLine("Displaying selected boids.");
        break;

    default:
        Console.WriteLine("Unknown display mode.");
        break;
}
```

Each `case` handles a matching value. `break` exits the switch statement. `default` handles values that don't match another case.

A switch expression returns a value:

```csharp
char displayMode = 'A';

string description = displayMode switch
{
    'A' => "All boids",
    'S' => "Selected boids",
    _ => "Unknown mode"
};

Console.WriteLine(description);
```

The `_` pattern acts as the default case. Switch expressions are useful when the purpose is to calculate a result rather than execute a series of statements.

### 4. For Loops

A `for` loop is useful when you know how many times you want to repeat an operation.

```csharp
for (int i = 0; i < 5; i++)
{
    Console.WriteLine(i);
}
```

Output:

```text
0
1
2
3
4
```

The loop has three main parts:

- **Initialization:** `int i = 0` runs once before the loop.
- **Condition:** `i < 5` is checked before each iteration.
- **Iterator:** `i++` runs after each completed iteration.

The loop stops when the condition becomes false.

A `for` loop is commonly used when iterating by index or repeating an operation a known number of times.

For example, you might use a loop to run several simulation steps:

```csharp
for (int step = 0; step < 10; step++)
{
    Console.WriteLine($"Simulation step {step}");
}
```

### 5. While Loops

A `while` loop repeats as long as its condition is true.

```csharp
int remainingSteps = 3;

while (remainingSteps > 0)
{
    Console.WriteLine($"Steps remaining: {remainingSteps}");
    remainingSteps--;
}
```

The condition is checked before each iteration. If it starts false, the body never executes.

Make sure something inside the loop can eventually make the condition false. Otherwise, you may create an infinite loop.

### 6. Do-While Loops

A `do-while` loop checks its condition after executing the body.

```csharp
int attempts = 0;

do
{
    attempts++;
    Console.WriteLine($"Attempt {attempts}");
}
while (attempts < 3);
```

Unlike `while`, a `do-while` loop always executes its body at least once.

Use it when the operation must happen before the condition is checked, such as prompting for input before validating it.

### 7. Foreach Loops

A `foreach` loop iterates through each element in a collection without requiring you to manage an index manually.

```csharp
int[] boidCounts = { 10, 25, 50 };

foreach (int count in boidCounts)
{
    Console.WriteLine(count);
}
```

Each iteration assigns the current element to `count`.

A `foreach` loop is useful when you want to process every item in a collection. It is often clearer and less error-prone than manually managing an index.

For example, later we might iterate through every boid to update its state or calculate its behavior.

A `foreach` iteration variable is not a way to replace collection elements. If you need to update elements by index, use an indexed loop or another appropriate approach.

### 8. Break and Continue

`break` exits the nearest enclosing loop immediately.

```csharp
for (int i = 0; i < 10; i++)
{
    if (i == 5)
    {
        break;
    }

    Console.WriteLine(i);
}
```

This prints `0` through `4`, then exits the loop.

`continue` skips the remainder of the current iteration and proceeds to the next one.

```csharp
for (int i = 0; i < 5; i++)
{
    if (i == 2)
    {
        continue;
    }

    Console.WriteLine(i);
}
```

This prints `0`, `1`, `3`, and `4`.

Use `break` when no further iterations are needed. Use `continue` when an item should be skipped but the loop should keep processing the remaining items.

### 9. Nested Control Flow

Control-flow constructs can be combined.

```csharp
int[] boidCounts = { 0, 10, 100, 600 };
const int MaxBoids = 500;

foreach (int count in boidCounts)
{
    if (count == 0)
    {
        Console.WriteLine("No boids.");
    }
    else if (count <= MaxBoids)
    {
        Console.WriteLine($"Valid count: {count}");
    }
    else
    {
        Console.WriteLine($"Count exceeds the limit: {count}");
    }
}
```

This loop processes each count and selects the appropriate branch.

Nested logic is powerful, but excessive nesting can make code difficult to follow. Prefer clear conditions and early exits when they improve readability.

## Challenge

Create a console program that simulates a series of Boids simulation steps and validates a set of boid counts.

Your program must:

1. Use an `if`/`else if`/`else` chain to classify a simulation as empty, small (1–49 boids), medium (50–199 boids), or large (200 or more boids).
2. Use a switch statement or switch expression to translate a display mode character into a readable description.
3. Use a `for` loop to simulate 10 numbered steps, starting at step 1.
4. Use a `while` loop to count down from 3 to 1.
5. Use a `do-while` loop to demonstrate a body that executes at least once.
6. Use a `foreach` loop to process an array of boid counts.
7. Use `continue` to skip a chosen count and `break` to stop processing when a count exceeds the maximum allowed.
8. Print clear output showing which branches, loops, and control statements are executing.

Choose your own values. Make sure the output demonstrates that each construct behaves as expected.

Implement the program independently in `Program.cs`. When it runs, share your code for review before moving on.
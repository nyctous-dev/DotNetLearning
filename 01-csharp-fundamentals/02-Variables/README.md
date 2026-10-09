# Variables & Types

## Micro Goal

Understand how to declare, initialize, store, and use data in C#, and how the type of a variable determines what values it can hold.

## Concepts Covered

- **Variable declaration and initialization** — creating variables and assigning values.
- **Built-in types** — `int`, `double`, `decimal`, `bool`, `char`, and `string`.
- **Type inference** — using `var` when the compiler can infer the type.
- **Constants** — using `const` for values that cannot change.
- **Type conversion** — implicit conversions, explicit casts, and parsing strings into values.
- **Nullable types** — representing values that may be absent.

## Completion Criteria

Be able to choose an appropriate type for a value, declare and initialize variables, explain `var` versus explicit types, and safely convert between common types.

## Lecture

### 1. Variables

A variable is a named storage location that lets your program work with a value.

```csharp
type variableName = value;
```

For example:

```csharp
int boidCount = 100;
```

- `int` specifies the type.
- `boidCount` is the variable name.
- `=` assigns the value.
- `100` is the initial value.
- `;` terminates the statement.

Declaration and assignment can be separate:

```csharp
int boidCount;
boidCount = 100;
```

Local variables must be assigned before they are read. Once initialized, a variable can be reassigned, but its type remains fixed.

```csharp
int boidCount = 100;
boidCount = 150;
Local variables must be assigned before they are read. Once initialized, a variable can be reassigned, but its type remains fixed.

```

### 2. Built-in Types

C# is statically typed. A variable's type determines what kind of value it can hold and which operations are available.

**`int` — Whole numbers**

```csharp
int boidCount = 100;
int temperature = -5;
```

Stores signed 32-bit integers from −2,147,483,648 to 2,147,483,647. Use it for counts and indexes.

Integer division discards the fractional part:

```csharp
int result = 5 / 2; // 2
```

**`double` — Floating-point numbers**

```csharp
double speed = 2.5;
double xPosition = 145.75;
```

Represents fractional values with a large range. Use it for simulation positions, velocities, distances, and angles.

```csharp
double result = 5.0 / 2.0; // 2.5
```

Some decimal fractions cannot be represented exactly in binary floating-point. This is generally acceptable for simulation calculations.

**`decimal` — Decimal arithmetic**

```csharp
decimal price = 19.99m;
decimal taxRate = 0.08m;
```

The `m` suffix identifies a decimal literal. Use `decimal` when base-10 precision matters, such as financial calculations. `double` is generally more appropriate for our simulation.

**`bool` — Boolean values**

```csharp
bool isRunning = true;
bool isPaused = false;
```

A Boolean holds only `true` or `false`. Use it for states and decisions.

**`char` — A single UTF-16 code unit**

```csharp
char grade = 'A';
char symbol = '#';
```

Characters use single quotes. Some Unicode characters require more than one `char`.

**`string` — Text**

```csharp
string simulationName = "Boids";
string message = "Hello, World!";
```

Strings use double quotes and are immutable: their contents cannot be changed after creation. Operations that appear to modify a string produce a new string.

```csharp
string name = "Boids";
name = name + " Simulation";
```

The variable now refers to the resulting string.

### 3. Type Inference with `var`

C# can infer a variable's type from its initial value.

```csharp
var boidCount = 100;       // int
var speed = 2.5;           // double
var simulationName = "Boids"; // string
```

This is equivalent to declaring the types explicitly. **`var` does not make C# dynamically typed**; the compiler determines the type at compile time.

```csharp
var count = 100;
count = 200;       // Valid
// count = "many"; // Compile error
```

A `var` declaration must include an initializer because the compiler needs to infer the type.

Use `var` when the type is obvious from the assigned expression. Use explicit types when they make the code clearer.

### 4. Constants with `const`

A constant cannot be reassigned after declaration.

```csharp
const int MaxBoids = 500;
const double DefaultSpeed = 2.0;
```

Constants must be initialized when declared and must have compile-time constant values.

Use `const` for values that are genuinely fixed in code. Values that need to change at runtime or come from configuration should not be constants.

### 5. Type Conversion

**Implicit conversion**

Some conversions happen automatically when C# supports them without explicit syntax.

```csharp
int count = 100;
double preciseCount = count; // 100.0
```

Every `int` value is exactly representable as a `double`.

**Explicit casting**

A cast explicitly requests a conversion that may lose information.

```csharp
double speed = 2.9;
int wholeSpeed = (int)speed; // 2
```

Casting to `int` truncates the fractional part; it does not round to the nearest integer.

**Parsing strings**

Text input often needs to be converted into a numeric value.

```csharp
string input = "150";
int count = int.Parse(input);
```

`int.Parse` throws an exception if the input is invalid or outside the range of `int`.

Use `TryParse` when input might be invalid:

```csharp
string input = "150";

if (int.TryParse(input, out int count))
{
    Console.WriteLine($"Boid count: {count}");
}
else
{
    Console.WriteLine("Invalid integer.");
}
```

`TryParse` returns `true` when conversion succeeds and `false` when it fails.

A cast converts an existing value; parsing interprets text and attempts to create a value of the requested type.

### 6. Nullable Types

Sometimes a value may be absent.

**Nullable value types**

Value types such as `int`, `double`, and `bool` cannot normally hold `null`. Add `?` to allow the value to be absent.

```csharp
int? selectedBoidCount = null;
selectedBoidCount = 100;
selectedBoidCount = null;
```

Check whether a value is present with `HasValue`, or use `??` to provide a fallback:

```csharp
int? selectedBoidCount = null;
int count = selectedBoidCount ?? 100; // 100
```

**Nullable reference types**

Reference types such as `string` can also be null. With nullable reference types enabled, `string` indicates a non-null value is expected, while `string?` indicates that null is permitted.

```csharp
string simulationName = "Boids";
string? description = null;
```

Nullable reference types provide compile-time analysis and warnings; they do not create a separate runtime string type. Check for null before accessing members that require a non-null value.

### 7. Putting It Together

```csharp
string simulationName = "Boids";
const int MaxBoids = 500;

int boidCount = 100;
double speed = 2.5;
bool isRunning = false;
char displayMode = 'A';

string input = "150";

if (int.TryParse(input, out int requestedCount))
{
    boidCount = requestedCount;
}

int? selectedBoidCount = null;
int effectiveCount = selectedBoidCount ?? boidCount;

Console.WriteLine($"Simulation: {simulationName}");
Console.WriteLine($"Boids: {effectiveCount}");
Console.WriteLine($"Maximum boids: {MaxBoids}");
Console.WriteLine($"Speed: {speed}");
Console.WriteLine($"Running: {isRunning}");
Console.WriteLine($"Display mode: {displayMode}");
```

This combines variables, types, constants, inference, conversion, parsing, and nullable values.

## Challenge

Create a console program in `Program.cs` representing the initial configuration of a Boids simulation.

Your program must:

1. Declare variables for a simulation name, boid count, movement speed, and running state, using appropriate types.
2. Declare a `const` value for the maximum allowed boid count.
3. Use `var` for at least one variable.
4. Convert a numeric value from one type to another.
5. Parse a string into an integer using `int.TryParse`.
6. Declare a nullable variable and demonstrate a fallback value using `??`.
7. Print all resulting values to the console.

Implement the program independently rather than copying the combined example verbatim. Review the code against the completion criteria before moving on.
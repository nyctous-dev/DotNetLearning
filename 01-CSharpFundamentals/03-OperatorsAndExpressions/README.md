# Operators & Expressions

## Micro Goal

Understand how C# operators manipulate values, compare data, and combine conditions to produce results.

## Concepts Covered

- **Arithmetic operators** — performing mathematical calculations.
- **Assignment operators** — assigning and updating variable values.
- **Comparison operators** — comparing values and producing Boolean results.
- **Logical operators** — combining or reversing Boolean conditions.
- **Operator precedence** — understanding the order in which expressions are evaluated.
- **Increment and decrement** — increasing or decreasing numeric values.
- **Compound expressions** — combining operators to calculate meaningful results.

## Completion Criteria

Be able to construct and evaluate arithmetic and Boolean expressions, update variables with assignment operators, explain operator precedence, and use comparisons and logical operators to express conditions.

## Lecture

### 1. What Is an Operator?

An operator is a symbol that performs an operation on one or more values. An expression combines values, variables, and operators to produce a result.

```csharp
int boidCount = 100;
int additionalBoids = 20;

int totalBoids = boidCount + additionalBoids;
```

Here, `+` is an operator, and `boidCount + additionalBoids` is an expression that evaluates to `120`.

Operators can produce different kinds of results. Arithmetic operators commonly produce numeric values, while comparison operators produce `bool` values.

### 2. Arithmetic Operators

Arithmetic operators perform mathematical calculations.

| Operator | Operation | Example | Result |
|---|---|---|---|
| `+` | Addition | `10 + 3` | `13` |
| `-` | Subtraction | `10 - 3` | `7` |
| `*` | Multiplication | `10 * 3` | `30` |
| `/` | Division | `10.0 / 4.0` | `2.5` |
| `%` | Remainder | `10 % 3` | `1` |

The type of the operands matters. When both operands are integers, division produces an integer result:

```csharp
int result = 10 / 4; // 2
```

With floating-point operands, the fractional part is preserved:

```csharp
double result = 10.0 / 4.0; // 2.5
```

The remainder operator `%` returns the remainder after division. It can be useful for repeating patterns or checking divisibility:

```csharp
int frame = 12;

bool isEven = frame % 2 == 0; // true
```

Be careful when dividing by zero. Integer division by zero throws an exception, while floating-point division follows floating-point rules and can produce values such as infinity or `NaN`.

### 3. Assignment Operators

The assignment operator `=` assigns a value to a variable.

```csharp
int boidCount = 100;
boidCount = 150;
```

Compound assignment operators perform an operation and assign the result back to the variable.

| Operator | Equivalent form |
|---|---|
| `+=` | `x = x + value` |
| `-=` | `x = x - value` |
| `*=` | `x = x * value` |
| `/=` | `x = x / value` |
| `%=` | `x = x % value` |

For example:

```csharp
int boidCount = 100;

boidCount += 20; // 120
boidCount -= 10; // 110
boidCount *= 2;  // 220
```

Compound assignments are useful when updating simulation state, such as adjusting a count or changing a speed value.

Remember that `=` and `==` are different. `=` assigns a value; `==` compares two values for equality.

### 4. Increment and Decrement

The increment operator `++` increases a variable by one. The decrement operator `--` decreases it by one.

```csharp
int frame = 0;

frame++; // frame is now 1
frame--; // frame is now 0
```

When used as a standalone statement, prefix and postfix forms both update the variable by one.

When used inside a larger expression, their behavior differs:

```csharp
int x = 5;

int a = x++; // a is 5, x becomes 6

int b = ++x; // x becomes 7, b is 7
```

- `x++` evaluates to the old value, then increments `x`.
- `++x` increments `x` first, then evaluates to the new value.

For clarity, prefer using these operators as standalone statements unless the difference is intentional.

### 5. Comparison Operators

Comparison operators compare two values and produce a Boolean result: `true` or `false`.

| Operator | Meaning | Example |
|---|---|---|
| `==` | Equal to | `x == 10` |
| `!=` | Not equal to | `x != 10` |
| `>` | Greater than | `x > 10` |
| `<` | Less than | `x < 10` |
| `>=` | Greater than or equal to | `x >= 10` |
| `<=` | Less than or equal to | `x <= 10` |

Example:

```csharp
int boidCount = 150;
int maxBoids = 500;

bool isBelowLimit = boidCount < maxBoids; // true
bool isAtLimit = boidCount == maxBoids;   // false
bool exceedsLimit = boidCount > maxBoids; // false
```

Comparison results are especially useful in conditions and validation logic.

### 6. Logical Operators

Logical operators combine or reverse Boolean expressions.

| Operator | Meaning | Example |
|---|---|---|
| `&&` | Logical AND | `isRunning && hasBoids` |
| `||` | Logical OR | `isPaused || isStopped` |
| `!` | Logical NOT | `!isRunning` |

**AND (`&&`)** returns `true` only when both operands are true.

```csharp
bool isRunning = true;
bool hasBoids = true;

bool shouldUpdate = isRunning && hasBoids; // true
```

**OR (`||`)** returns `true` when at least one operand is true.

```csharp
bool isPaused = false;
bool isStopped = true;

bool shouldStop = isPaused || isStopped; // true
```

**NOT (`!`)** reverses a Boolean value.

```csharp
bool isRunning = true;
bool isStopped = !isRunning; // false
```

Logical operators use short-circuit evaluation:

- With `&&`, if the left operand is `false`, the right operand is not evaluated.
- With `||`, if the left operand is `true`, the right operand is not evaluated.

For example:

```csharp
int boidCount = 0;

bool hasBoids = boidCount > 0;
bool canUpdate = hasBoids && GetSimulationState();

static bool GetSimulationState()
{
    Console.WriteLine("Checking simulation state.");
    return true;
}
```

Because `hasBoids` is false, `GetSimulationState()` is not called. Short-circuiting can avoid unnecessary work and protect expressions that should only run under certain conditions.

### 7. Operator Precedence

When an expression contains multiple operators, C# follows rules that determine the order of evaluation.

```csharp
int result = 2 + 3 * 4; // 14
```

Multiplication takes precedence over addition, so the expression is evaluated as `2 + (3 * 4)`.

Parentheses make the intended order explicit:

```csharp
int result = (2 + 3) * 4; // 20
```

A simplified precedence guide, from higher to lower, is:

1. Parentheses and member access
2. Unary operators, such as `!`, `-`, and `++`
3. Multiplication, division, and remainder: `*`, `/`, `%`
4. Addition and subtraction: `+`, `-`
5. Comparisons: `<`, `>`, `<=`, `>=`
6. Equality: `==`, `!=`
7. Logical AND: `&&`
8. Logical OR: `||`
9. Assignment: `=`, `+=`, `-=`, and similar operators

This is a simplified guide rather than a complete language specification. When an expression is difficult to read, use parentheses instead of relying on memory.

For example:

```csharp
int boidCount = 100;
int maxBoids = 500;
bool isRunning = true;

bool canAddBoids = isRunning && boidCount < maxBoids;
```

The comparison `boidCount < maxBoids` is evaluated before the logical AND combines its result with `isRunning`.

### 8. Compound Expressions in a Simulation

Operators become more useful when combined to calculate values and make decisions.

Suppose the simulation has a boid count, a maximum capacity, and a speed multiplier:

```csharp
int boidCount = 100;
const int MaxBoids = 500;

double baseSpeed = 2.0;
double speedMultiplier = 1.5;

double movementSpeed = baseSpeed * speedMultiplier;
bool canAddBoids = boidCount < MaxBoids;
```

Here, multiplication calculates the movement speed, and comparison determines whether the simulation has capacity for additional boids.

A more complex condition could be:

```csharp
bool isRunning = true;
bool isPaused = false;

bool shouldUpdate = isRunning && !isPaused && boidCount > 0;
```

The update should occur only if the simulation is running, isn't paused, and has at least one boid.

This is the foundation of expressing simulation rules in code.

## Challenge

Create a program in `Program.cs` that models basic Boids simulation settings using operators and expressions.

Your program must:

1. Declare a boid count, a maximum boid count, a base speed, a speed multiplier, and a running state.
2. Use arithmetic operators to calculate the total movement speed.
3. Use compound assignment to increase the boid count.
4. Use comparison operators to determine whether the boid count is below the maximum.
5. Use logical operators to determine whether the simulation should update. It should be running, not paused, and have at least one boid.
6. Use the remainder operator to determine whether the boid count is even or odd.
7. Include one expression involving multiple operators and use parentheses to make its evaluation order clear.
8. Print the calculated values and Boolean results to the console.

Choose your own initial values. Make sure your output is consistent with those values and the operators you use.

Implement the solution independently. When your program runs, share your `Program.cs` and we'll review it against the completion criteria before moving on.
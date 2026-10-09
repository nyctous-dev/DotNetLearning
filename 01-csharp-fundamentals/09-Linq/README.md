# LINQ

## Micro Goal

Understand how to query, filter, sort, transform, group, and aggregate data in C# using Language Integrated Query (LINQ), and apply these operations to collections of objects in a Boids simulation.

## Concepts Covered

- Language Integrated Query (LINQ)
- Query syntax and method syntax
- `Where` for filtering
- `Select` for projection
- `OrderBy`, `OrderByDescending`, and `ThenBy`
- `First`, `FirstOrDefault`, `Single`, and their variants
- `Any` and `All`
- `Count`, `Sum`, `Min`, `Max`, and `Average`
- `Distinct` and `DistinctBy`
- `GroupBy`
- Deferred execution and immediate execution
- Materializing queries with `ToList` and `ToArray`
- LINQ with custom objects
- Chaining LINQ operations
- Common LINQ mistakes and performance considerations

## Completion Criteria

- [ ] Explain what LINQ is and why it is useful.
- [ ] Filter a collection using `Where`.
- [ ] Transform objects or values using `Select`.
- [ ] Sort query results using ordering operators.
- [ ] Use `Any` and `All` to test conditions.
- [ ] Use aggregate operators to calculate statistics.
- [ ] Retrieve elements safely when a matching result may not exist.
- [ ] Group objects using `GroupBy`.
- [ ] Explain deferred execution and materialize a query when necessary.
- [ ] Build chained queries over a collection of Boid objects.
- [ ] Complete the challenge using LINQ without unnecessarily replacing simple operations with complex queries.

## Lecture

### 1. What is LINQ?

LINQ stands for **Language Integrated Query**. It provides a consistent way to query data from collections and other supported data sources using C#.

Without LINQ, filtering a collection often requires manually creating another collection and writing a loop:

```csharp
List<int> speeds = new() { 1, 3, 5, 2, 4 };
List<int> fastSpeeds = new();

foreach (int speed in speeds)
{
    if (speed >= 3)
    {
        fastSpeeds.Add(speed);
    }
}
```

LINQ expresses the same operation more concisely:

```csharp
List<int> speeds = new() { 1, 3, 5, 2, 4 };

List<int> fastSpeeds = speeds
    .Where(speed => speed >= 3)
    .ToList();
```

Both versions produce a list containing `3`, `5`, and `4`, in that order.

LINQ is useful because it separates the description of a query from the code needed to manually implement its iteration. It also supports chaining operations, so a query can filter, transform, sort, and aggregate data in a readable sequence.

LINQ works with arrays, lists, and many other data sources. In this module, the focus is on LINQ to Objects: querying in-memory collections using C#.

### 2. Query syntax and method syntax

LINQ supports two main forms of syntax.

**Method syntax** calls LINQ methods such as `Where` and `Select`:

```csharp
int[] speeds = { 1, 3, 5, 2, 4 };

var fastSpeeds = speeds.Where(speed => speed >= 3);
```

**Query syntax** resembles a database query:

```csharp
int[] speeds = { 1, 3, 5, 2, 4 };

var fastSpeeds =
    from speed in speeds
    where speed >= 3
    select speed;
```

Both queries represent the same filtering operation.

Method syntax is commonly used in C# applications, particularly when chaining operations. Query syntax can be useful for queries with multiple joins or complex grouping. Both are valid LINQ, and some query expressions are translated into method calls by the compiler.

For this module, use method syntax as the default. Learn to recognize query syntax, but do not mix the two styles without a reason.

### 3. Lambda expressions

Many LINQ methods accept a lambda expression: a concise way to define a function.

```csharp
speed => speed >= 3
```

This expression takes a value called `speed` and returns whether it is at least `3`.

A lambda can also use a block body:

```csharp
speed =>
{
    double adjustedSpeed = speed * 1.5;
    return adjustedSpeed >= 3;
}
```

For simple operations, prefer the concise expression form. The block form is useful when several statements are genuinely necessary.

LINQ uses lambda expressions to describe what should happen to each element. For example, `Where` uses a predicate that returns a Boolean, while `Select` uses a function that returns a transformed value.

### 4. Filtering with `Where`

`Where` returns the elements that satisfy a condition.

```csharp
int[] boidCounts = { 10, 25, 50, 100, 200 };

var largeGroups = boidCounts.Where(count => count >= 50);
```

The query represents the values `50`, `100`, and `200`.

`Where` does not change the original array. It produces a query that can enumerate the matching elements.

You can chain conditions using logical operators:

```csharp
var mediumGroups = boidCounts
    .Where(count => count >= 25 && count < 100);
```

This selects `25` and `50`.

Use `Where` when you need all matching elements. If you only need to know whether at least one matching element exists, `Any` may express the intent more directly.

### 5. Transforming with `Select`

`Select` projects each element into a new value. Projection means transforming one representation into another.

For example, convert speeds from meters per second to kilometers per hour:

```csharp
double[] speeds = { 1.0, 2.0, 3.0 };

var speedsInKilometersPerHour = speeds
    .Select(speed => speed * 3.6);
```

The resulting values are `3.6`, `7.2`, and `10.8`.

You can also project objects into strings or anonymous objects:

```csharp
var boids = new[]
{
    new { Id = 1, X = 10.0, Y = 20.0 },
    new { Id = 2, X = 30.0, Y = 40.0 }
};

var descriptions = boids.Select(
    boid => $"Boid {boid.Id} at ({boid.X}, {boid.Y})");
```

The result is a sequence of strings, not a sequence of boid objects.

`Where` determines which elements remain. `Select` determines what each output element contains. These operations are often used together.

### 6. Sorting with `OrderBy` and `ThenBy`

`OrderBy` sorts a sequence by a key in ascending order. `OrderByDescending` sorts in descending order.

```csharp
double[] speeds = { 3.0, 1.5, 2.5, 1.0 };

var ascending = speeds.OrderBy(speed => speed);
var descending = speeds.OrderByDescending(speed => speed);
```

For objects, sort by a property:

```csharp
var boids = new[]
{
    new { Id = 1, Speed = 2.0 },
    new { Id = 2, Speed = 1.0 },
    new { Id = 3, Speed = 2.0 }
};

var sortedBoids = boids
    .OrderBy(boid => boid.Speed)
    .ThenBy(boid => boid.Id);
```

`ThenBy` supplies a secondary sort key. Here, boids are ordered by speed first, then by identifier when speeds are equal.

Sorting with these operators produces an ordered sequence; it does not rearrange the original collection in place.

### 7. Testing conditions with `Any` and `All`

`Any` determines whether at least one element satisfies a condition:

```csharp
double[] speeds = { 1.0, 2.0, 3.0 };

bool hasFastBoid = speeds.Any(speed => speed >= 3.0);
```

The result is `true`.

`All` determines whether every element satisfies a condition:

```csharp
bool allSpeedsArePositive = speeds.All(speed => speed > 0);
```

The result is also `true`.

An important detail: `All` returns `true` for an empty sequence because there are no elements that violate the condition. If your program requires a nonempty collection, check that separately.

You can also call `Any()` without a predicate to test whether a sequence contains at least one element.

Use these operators for questions about a collection rather than retrieving and counting elements unnecessarily.

### 8. Counting and aggregation

LINQ provides methods that summarize a sequence.

```csharp
double[] speeds = { 1.0, 2.0, 3.0, 4.0 };

int count = speeds.Count();
double total = speeds.Sum();
double minimum = speeds.Min();
double maximum = speeds.Max();
double average = speeds.Average();
```

The results are:

- Count: `4`
- Sum: `10`
- Minimum: `1`
- Maximum: `4`
- Average: `2.5`

You can supply a predicate to `Count`:

```csharp
int fastCount = speeds.Count(speed => speed >= 3.0);
```

This counts the elements that meet the condition.

For object collections, a selector can identify the values to aggregate:

```csharp
var boids = new[]
{
    new { Id = 1, Speed = 2.0 },
    new { Id = 2, Speed = 4.0 },
    new { Id = 3, Speed = 3.0 }
};

double averageSpeed = boids.Average(boid => boid.Speed);
```

`Average`, `Min`, and `Max` have overloads that operate on different numeric types. Empty-sequence behavior matters: `Average` on an empty sequence of non-nullable numeric values throws an `InvalidOperationException`; `Min` and `Max` behavior depends on the overload and element type. Check whether a sequence has elements before aggregating when emptiness is possible.

### 9. Retrieving individual elements

LINQ includes methods for retrieving one element from a sequence.

- `First()` returns the first element and throws if the sequence is empty.
- `FirstOrDefault()` returns the first element or the default value if no element exists.
- `Single()` requires exactly one element and throws if there are zero or multiple elements.
- `SingleOrDefault()` allows zero or one element but throws if there are multiple elements.

Each has predicate overloads that search for an element matching a condition.

For example:

```csharp
int[] boidCounts = { 10, 25, 50, 100 };

int firstLargeCount = boidCounts.First(count => count >= 50);
```

This returns `50`.

If no value meets the condition, `First` throws. If a match may not exist, use a safe alternative:

```csharp
int? firstVeryLargeCount = boidCounts
    .Where(count => count > 500)
    .Select(count => (int?)count)
    .FirstOrDefault();
```

This returns `null` when no matching value exists. The nullable projection is useful here because `FirstOrDefault()` on a non-nullable integer sequence would return `0`, which can be indistinguishable from a legitimate result.

Use `Single` when exactly one result is required by the data model, such as a supposedly unique match. Do not use it merely because you expect a sequence to contain one element; it enforces that expectation at runtime.

### 10. Removing duplicates with `Distinct` and `DistinctBy`

`Distinct` returns unique values:

```csharp
int[] identifiers = { 1, 2, 2, 3, 3, 3 };

var uniqueIdentifiers = identifiers.Distinct();
```

The resulting sequence contains `1`, `2`, and `3`.

For objects, `Distinct` uses the type's equality behavior. If the class does not define value equality, two separate instances may be treated as different even if their properties match.

`DistinctBy` selects uniqueness based on a key:

```csharp
var boids = new[]
{
    new { Id = 1, Speed = 2.0 },
    new { Id = 1, Speed = 3.0 },
    new { Id = 2, Speed = 1.5 }
};

var uniqueBoids = boids.DistinctBy(boid => boid.Id);
```

This keeps one element for each identifier. `DistinctBy` is available in modern .NET versions, including .NET 10.

Use `DistinctBy` when uniqueness is defined by a particular property rather than by the equality of the entire object.

### 11. Grouping with `GroupBy`

`GroupBy` partitions a sequence into groups that share a key.

```csharp
var boids = new[]
{
    new { Id = 1, Speed = 1.0 },
    new { Id = 2, Speed = 2.5 },
    new { Id = 3, Speed = 1.5 },
    new { Id = 4, Speed = 3.0 }
};

var speedGroups = boids.GroupBy(
    boid => boid.Speed >= 2.0 ? "Fast" : "Slow");

foreach (var group in speedGroups)
{
    Console.WriteLine($"{group.Key}:");

    foreach (var boid in group)
    {
        Console.WriteLine($"  Boid {boid.Id}: {boid.Speed}");
    }
}
```

The key is either `"Fast"` or `"Slow"`. Each group contains the boids assigned to that key.

A grouping is useful when you need to summarize or process categories separately, such as grouping boids by speed range or simulation state.

You can aggregate each group:

```csharp
var summaries = boids
    .GroupBy(boid => boid.Speed >= 2.0 ? "Fast" : "Slow")
    .Select(group => new
    {
        Category = group.Key,
        Count = group.Count(),
        AverageSpeed = group.Average(boid => boid.Speed)
    });
```

This produces a summary per category. Grouping is powerful, but use it only when grouping is part of the problem; a simple filter is clearer when you need just one subset.

### 12. Deferred execution

Many LINQ operators, including `Where`, `Select`, and `OrderBy`, use deferred execution. The query is defined first, but the work is performed when the results are enumerated.

```csharp
List<int> counts = new() { 10, 20, 30 };

var largeCounts = counts.Where(count => count >= 20);

counts.Add(40);

foreach (int count in largeCounts)
{
    Console.WriteLine(count);
}
```

The output includes `20`, `30`, and `40`, because the query is evaluated when the `foreach` loop enumerates it, after the new element has been added.

Deferred execution can be useful because it avoids calculating results until they are needed. However, it also means a query may produce different results when the underlying collection changes between enumerations.

Some LINQ operations execute immediately. Examples include `Count()`, `Sum()`, `Average()`, `Any()`, and materialization with `ToList()` or `ToArray()`.

### 13. Materializing query results

Use `ToList()` or `ToArray()` to execute a query and store its results in a new collection.

```csharp
List<int> counts = new() { 10, 20, 30 };

List<int> largeCounts = counts
    .Where(count => count >= 20)
    .ToList();

counts.Add(40);
```

The `largeCounts` list contains `20` and `30`; adding `40` to the original collection does not add it to the already materialized result.

Materialization is useful when you need a stable snapshot of the results, need to iterate repeatedly without rerunning the query, or need a concrete list or array for another method.

Do not materialize every query automatically. `ToList()` allocates storage for the results, so keeping a query deferred may be more efficient when the results only need to be enumerated once.

### 14. Chaining LINQ operations

LINQ operators can be chained to describe a sequence of transformations.

```csharp
var boids = new[]
{
    new { Id = 1, Speed = 1.0 },
    new { Id = 2, Speed = 2.5 },
    new { Id = 3, Speed = 3.0 },
    new { Id = 4, Speed = 1.5 }
};

var fastBoidIds = boids
    .Where(boid => boid.Speed >= 2.0)
    .OrderByDescending(boid => boid.Speed)
    .Select(boid => boid.Id)
    .ToList();
```

The query:

1. Filters to boids with speed at least `2.0`.
2. Sorts them from fastest to slowest.
3. Projects each result to its identifier.
4. Materializes the identifiers into a list.

The result is `[3, 2]`.

The order of operations matters. Filtering before sorting can avoid sorting elements that will later be discarded. Projection before filtering may change which information is available to the predicate. Arrange the chain so that each step clearly expresses the intended result.

### 15. LINQ with custom objects

LINQ becomes particularly useful when querying objects rather than simple numbers.

Suppose a `Boid` class exposes `Id`, `X`, `Y`, and `Speed`. You could find boids moving faster than a threshold, order them by speed, and select their identifiers:

```csharp
List<Boid> boids = new()
{
    new Boid(1, 10.0, 20.0, 1.5),
    new Boid(2, 30.0, 40.0, 3.0),
    new Boid(3, 50.0, 60.0, 2.5)
};

var fastBoidIds = boids
    .Where(boid => boid.Speed >= 2.5)
    .OrderByDescending(boid => boid.Speed)
    .Select(boid => boid.Id)
    .ToList();
```

This assumes a `Boid` class with a constructor matching those arguments and the corresponding properties.

LINQ does not require a special class or collection type. It queries objects through the members available on their types.

Be aware that a query over mutable objects reads their property values when the query executes. If an object's speed changes before a deferred query is enumerated, the result may differ from what you would get by evaluating it earlier.

### 16. Common LINQ mistakes and performance considerations

Several common mistakes can make LINQ code incorrect or harder to maintain:

- **Assuming a query executes immediately:** `Where` and `Select` usually defer execution. Enumerating the query later can produce different results if the source changes.
- **Using `First()` when no match is guaranteed:** It throws if no element matches. Use `FirstOrDefault()` or another explicit strategy when absence is expected.
- **Using `Single()` when duplicates are possible:** It throws if more than one matching element exists.
- **Confusing filtering and projection:** `Where` retains elements; `Select` transforms them.
- **Sorting unnecessarily:** Sorting has a cost. If the task only requires finding the maximum value, `Max` or `MaxBy` may be more appropriate.
- **Enumerating expensive queries repeatedly:** If a query is costly and its result needs to be reused, materialize it once.
- **Creating excessively long chains:** LINQ should improve readability. Break a complex query into named intermediate variables when that makes the intent easier to understand.
- **Assuming `Distinct` compares every object's properties:** Equality depends on the type's equality implementation. Use `DistinctBy` when uniqueness is based on a specific key.

LINQ is a tool for expressing data operations, not a requirement to replace every loop. A `foreach` loop is often clearer when the task involves side effects, multiple steps of imperative logic, or complex state changes. Use LINQ primarily for querying and transforming sequences.

## Challenge

Build a console program that queries a collection of boids using LINQ. Use the `Boid` class from the previous module or define a minimal equivalent with an identifier, X and Y coordinates, and speed.

Your program must satisfy all of the following requirements:

1. **Create a collection:** Initialize a `List<Boid>` containing at least six boids with different identifiers, positions, and speeds.
2. **Filter:** Use `Where` to retrieve boids whose speed is at least a chosen threshold.
3. **Project:** Use `Select` to produce a collection of readable strings containing each selected boid's identifier and speed.
4. **Sort:** Use `OrderByDescending` to order boids from fastest to slowest.
5. **Test conditions:** Use `Any` to determine whether any boid exceeds a specified speed, and `All` to determine whether every boid has a non-negative speed.
6. **Aggregate:** Calculate the total number of boids and their average speed. Handle the possibility of an empty collection if your implementation allows one.
7. **Retrieve safely:** Use `FirstOrDefault` to find a boid meeting a condition and handle the case where no match exists.
8. **Group:** Use `GroupBy` to divide boids into at least two speed categories, then print the number of boids in each category.
9. **Demonstrate deferred execution:** Define a query, modify the source list before enumerating the query, and show that the new element appears when it meets the filter.
10. **Materialize results:** Use `ToList()` to create a snapshot of a query, modify the source afterward, and show that the materialized list does not automatically change.
11. **Keep the code readable:** Label each output section, use meaningful variable names, and avoid redundant queries or unnecessary intermediate collections.

The challenge should demonstrate the behavior of LINQ rather than merely print hard-coded answers. Use the simplest operator that expresses each requirement, and keep the object model separate from the query logic.
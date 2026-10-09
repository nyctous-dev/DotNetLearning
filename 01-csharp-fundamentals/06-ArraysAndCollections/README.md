# Arrays and Collections

## Micro Goal

Understand how to store, access, modify, and iterate over multiple values using arrays and common C# collection types, and know when to choose each structure.

## Concepts Covered

- Arrays and fixed-size storage
- Indexing and zero-based positions
- Reading and modifying array elements
- Array properties and methods
- `foreach` and `for` loops with collections
- Multidimensional arrays
- Lists with `List<T>`
- Adding, removing, searching, and counting elements
- Dictionaries with `Dictionary<TKey, TValue>`
- Sets with `HashSet<T>`
- Choosing the right collection for a task
- Common collection errors and safe access

## Completion Criteria

- [ ] Declare, initialize, read, and modify an array.
- [ ] Explain why array indexes start at zero and what happens when an index is out of range.
- [ ] Iterate through an array using both `for` and `foreach`.
- [ ] Create and modify a `List<T>` using common collection methods.
- [ ] Use a `Dictionary<TKey, TValue>` to associate keys with values.
- [ ] Use a `HashSet<T>` to store unique values.
- [ ] Choose an appropriate collection for a given requirement.
- [ ] Complete the challenge without putting all the logic directly into the top-level statements.

## Lecture

### 1. Why collections exist

A variable stores one value:

```csharp
int boidCount = 100;
```

But a simulation needs to manage many boids, their positions, their speeds, and other information. Creating a separate variable for every boid would be impractical.

Collections store multiple values under one variable. Different collection types provide different capabilities, such as fixed-size storage, dynamic resizing, key-based lookup, or automatic uniqueness.

The primary collections covered here are:

- `T[]`: an array, which has a fixed length.
- `List<T>`: a list, which can grow and shrink.
- `Dictionary<TKey, TValue>`: a collection that associates keys with values.
- `HashSet<T>`: a collection that stores unique values.

The `T`, `TKey`, and `TValue` notation represents generic type parameters. For example, `List<int>` is a list of integers, while `Dictionary<string, double>` associates strings with decimal-point numbers represented by `double`.

### 2. Arrays

An array stores a fixed number of elements of the same type. Its length is established when the array is created and cannot subsequently change.

You can declare and initialize an array like this:

```csharp
int[] boidCounts = { 10, 20, 30, 40 };
```

The type is `int[]`, meaning an array of integers. The initializer creates four elements.

You can also specify the length and assign values afterward:

```csharp
double[] speeds = new double[3];

speeds[0] = 1.5;
speeds[1] = 2.0;
speeds[2] = 2.5;
```

Creating an array with `new double[3]` allocates three elements. Numeric elements initially contain their type's default value, which is `0.0` for `double`.

Arrays use **zero-based indexing**. The first element is at index `0`, and the last element is at index `Length - 1`.

```csharp
Console.WriteLine(boidCounts[0]); // 10
Console.WriteLine(boidCounts[3]); // 40

boidCounts[1] = 25;
Console.WriteLine(boidCounts[1]); // 25
```

Accessing an invalid index, such as `boidCounts[4]`, throws an `IndexOutOfRangeException` at runtime. The array has four elements, but its valid indexes are `0` through `3`.

Use the `Length` property to determine the number of elements:

```csharp
Console.WriteLine(boidCounts.Length); // 4
```

An array's length is not the same as the index of its last element. For a nonempty array, the last valid index is one less than its length. An empty array has a length of zero and no valid element indexes.

### 3. Iterating through arrays

A `for` loop is useful when you need the index as well as the element:

```csharp
int[] boidCounts = { 10, 20, 30, 40 };

for (int index = 0; index < boidCounts.Length; index++)
{
    Console.WriteLine($"Index {index}: {boidCounts[index]}");
}
```

The condition `index < boidCounts.Length` prevents the loop from accessing an index beyond the array's end.

A `foreach` loop is simpler when you only need each value:

```csharp
foreach (int count in boidCounts)
{
    Console.WriteLine(count);
}
```

The loop variable `count` receives each element's value in turn. For an array of integers, changing the local variable does not change the array element:

```csharp
foreach (int count in boidCounts)
{
    // This would only change the local variable:
    // count = count + 1;
}
```

In this example, the assignment is commented out because a `foreach` iteration variable cannot be reassigned in this way. To modify array elements, use a `for` loop and assign through the index:

```csharp
for (int index = 0; index < boidCounts.Length; index++)
{
    boidCounts[index] += 1;
}
```

Choose `for` when you need an index or must replace elements. Choose `foreach` when you simply need to process each element.

### 4. Array utility methods

The `Array` class provides static methods for common operations. For example, `Array.Sort` sorts an array in place, while `Array.Reverse` reverses its element order in place.

```csharp
int[] counts = { 30, 10, 40, 20 };

Array.Sort(counts);

foreach (int count in counts)
{
    Console.WriteLine(count);
}
```

Output:

```text
10
20
30
40
```

You can search for an element using `Array.IndexOf`:

```csharp
int index = Array.IndexOf(counts, 30);
Console.WriteLine(index); // 2
```

If the value is not found, `Array.IndexOf` returns `-1`.

These operations do not change the array's length. Sorting rearranges elements; it does not add or remove them.

### 5. Multidimensional arrays

A multidimensional array stores values across more than one dimension. A rectangular two-dimensional array can represent rows and columns:

```csharp
int[,] grid = new int[2, 3];

grid[0, 0] = 1;
grid[0, 1] = 2;
grid[1, 2] = 3;

Console.WriteLine(grid[1, 2]); // 3
Console.WriteLine(grid.GetLength(0)); // 2 rows
Console.WriteLine(grid.GetLength(1)); // 3 columns
```

A two-dimensional array uses two indexes, each starting at zero. `GetLength(0)` returns the number of rows, and `GetLength(1)` returns the number of columns.

You can iterate over both dimensions with nested loops:

```csharp
for (int row = 0; row < grid.GetLength(0); row++)
{
    for (int column = 0; column < grid.GetLength(1); column++)
    {
        Console.WriteLine(grid[row, column]);
    }
}
```

Multidimensional arrays are useful for grid-based data, but they are not necessary for every simulation. For example, a collection of boid objects is usually better represented by a list or array of boid instances.

### 6. Lists with `List<T>`

An array's fixed length is useful when the number of elements is known and stable. When the number of elements needs to change, use `List<T>`.

`List<T>` is part of the `System.Collections.Generic` namespace. In modern .NET console templates, common namespaces are often available through implicit global usings. If the compiler does not recognize `List<T>`, add:

```csharp
using System.Collections.Generic;
```

Create a list like this:

```csharp
List<int> boidCounts = new List<int>();

boidCounts.Add(10);
boidCounts.Add(20);
boidCounts.Add(30);
```

You can also use target-typed construction:

```csharp
List<int> boidCounts = new();
```

The list grows as elements are added. Its `Count` property reports how many elements it currently contains:

```csharp
Console.WriteLine(boidCounts.Count); // 3
```

Do not confuse `Count` with `Capacity`. `Count` is the number of stored elements. `Capacity` is the amount of storage currently allocated internally, which can be greater than `Count`.

Common list operations include:

```csharp
boidCounts.Add(40);        // Add one element
boidCounts.Insert(1, 15);  // Insert at index 1
boidCounts.Remove(20);     // Remove the first matching value
boidCounts.RemoveAt(0);    // Remove the element at index 0
bool containsThirty = boidCounts.Contains(30);
int index = boidCounts.IndexOf(30);
```

`Remove` returns a Boolean indicating whether a matching element was removed. `RemoveAt` removes by index and throws an `ArgumentOutOfRangeException` if the index is invalid.

Like arrays, lists use zero-based indexing:

```csharp
List<string> names = new() { "Alpha", "Beta", "Gamma" };

Console.WriteLine(names[0]); // Alpha

names[1] = "Delta";
Console.WriteLine(names[1]); // Delta
```

A list supports indexed reads and writes, but it does not automatically protect you from invalid indexes. Check `Count` when an index might be invalid.

A list can be iterated with `foreach`:

```csharp
foreach (int count in boidCounts)
{
    Console.WriteLine(count);
}
```

Avoid adding or removing elements from a list while iterating over that same list with `foreach`. Such structural changes invalidate the enumerator and normally cause an `InvalidOperationException` on a subsequent iteration step. If you need to remove elements during iteration, use an appropriate index-based strategy or build a separate filtered list.

### 7. Dictionaries with `Dictionary<TKey, TValue>`

A dictionary associates each key with a value. Use it when you need to look up a value by a meaningful identifier rather than by its position.

For example, you might associate each boid's identifier with its speed:

```csharp
Dictionary<string, double> speeds = new();

speeds["boid-001"] = 1.5;
speeds["boid-002"] = 2.0;

Console.WriteLine(speeds["boid-001"]); // 1.5
```

The key type here is `string`, and the value type is `double`.

Assigning a value to an existing key updates that entry. Adding a new key creates an entry. Dictionary keys must be unique; assigning through the indexer is different from calling `Add`.

```csharp
speeds.Add("boid-003", 2.5);
```

Calling `Add` with a key that already exists throws an `ArgumentException`. Use `ContainsKey` or `TryGetValue` when a key may not exist.

The indexer throws a `KeyNotFoundException` if the key is absent:

```csharp
if (speeds.TryGetValue("boid-001", out double speed))
{
    Console.WriteLine($"Speed: {speed}");
}
else
{
    Console.WriteLine("Boid not found.");
}
```

`TryGetValue` is useful because it checks for the key and retrieves the value in one operation. The `out` variable receives the value when the lookup succeeds.

You can iterate over dictionary entries:

```csharp
foreach (KeyValuePair<string, double> entry in speeds)
{
    Console.WriteLine($"{entry.Key}: {entry.Value}");
}
```

A dictionary is appropriate when you need key-based lookup. It is not a replacement for a list when ordering by position or processing a sequence is the main requirement.

### 8. Sets with `HashSet<T>`

A `HashSet<T>` stores unique values. Adding a value that is already present does not create a duplicate.

```csharp
HashSet<string> activeBoids = new();

activeBoids.Add("boid-001");
activeBoids.Add("boid-002");

bool addedAgain = activeBoids.Add("boid-001");

Console.WriteLine(activeBoids.Count); // 2
Console.WriteLine(addedAgain);        // False
```

`Add` returns `true` when a new element is added and `false` when the value was already present.

A set is useful when you need to track membership or prevent duplicates, such as tracking unique identifiers or recording which boids have already been processed.

You can test membership with `Contains` and remove values with `Remove`:

```csharp
bool isActive = activeBoids.Contains("boid-002");
activeBoids.Remove("boid-002");
```

A `HashSet<T>` does not provide index-based access, and you should not rely on its iteration order. Use a list or array when position and sequence matter.

### 9. Choosing a collection

The collection type should reflect how the data will be used.

| Requirement | Suitable collection | Reason |
|---|---|---|
| Fixed number of same-type elements | `T[]` | Fixed length and direct indexing |
| Number of elements changes | `List<T>` | Supports adding and removing elements |
| Look up values by unique key | `Dictionary<TKey, TValue>` | Associates keys with values |
| Store unique values and test membership | `HashSet<T>` | Prevents duplicate entries |

All four collections are strongly typed. A `List<int>` stores integers, while a `Dictionary<string, double>` maps strings to doubles. The compiler helps prevent accidentally placing a value of the wrong type into a collection.

Collections store values or references according to their element types. A collection of value types, such as `List<int>`, stores integer values. A collection of reference types, such as `List<Boid>`, stores references to `Boid` objects. This distinction matters when working with mutable objects, but the collection operations remain similar.

For a Boids simulation, a `List<Boid>` is a reasonable choice when the number of boids can change during execution. An array can be suitable when the number of boids is fixed or when a stable, fixed-size buffer is needed. A dictionary can map IDs to boids, while a set can track unique IDs.

### 10. Common mistakes and safe access

Several common errors are worth recognizing:

- **Using an invalid index:** Arrays and lists throw exceptions when an index is outside the valid range.
- **Confusing `Length` and `Count`:** Arrays use `Length`; lists, dictionaries, and sets use `Count`.
- **Assuming a list index is always valid:** A list can shrink after an index was recorded. Recheck its current `Count` before indexing when the collection may have changed.
- **Assuming dictionary keys exist:** Use `TryGetValue` if a key might be missing.
- **Expecting duplicates in a set:** `HashSet<T>` stores unique elements.
- **Changing a collection during `foreach`:** Adding or removing elements from the iterated collection can invalidate its enumerator.
- **Assuming every collection preserves the same order:** Arrays and lists are index-based sequences; dictionaries and sets should be chosen primarily for their lookup and membership behavior.

The central principle is to select a collection based on the operations your program needs. A suitable collection makes the code simpler, safer, and easier to maintain.

## Challenge

Build a small console program that manages boid identifiers and their movement speeds. Use arrays, lists, dictionaries, and sets to demonstrate the differences between these collection types.

Your program must satisfy all of the following requirements:

1. **Create an array:** Initialize an array containing at least four starting boid speeds. Print its length and every speed using a `for` loop.
2. **Modify an array:** Increase each speed by a fixed amount using an index-based loop, then print the updated speeds.
3. **Create a list:** Initialize a `List<string>` with at least three boid identifiers. Add another identifier, remove one identifier, and print the final count and remaining identifiers.
4. **Search a list:** Use `Contains` to determine whether a specified identifier is present and print the result.
5. **Create a dictionary:** Use `Dictionary<string, double>` to associate at least three boid identifiers with movement speeds. Retrieve one speed safely with `TryGetValue` and handle a missing identifier without throwing an exception.
6. **Create a set:** Use `HashSet<string>` to add identifiers, including one duplicate. Print whether the duplicate was added and how many unique identifiers remain.
7. **Choose appropriate structures:** Explain in comments why the array, list, dictionary, and set each suit their respective tasks.
8. **Use methods:** Implement separate methods for printing the speed array and reporting whether an identifier exists in the list. Call both methods from the top-level statements.
9. **Print clear output:** Label each section so the results are easy to interpret.

Keep the code focused on collection operations. Use descriptive variable names, appropriate types, and straightforward control flow. Do not put every operation inside a single method.
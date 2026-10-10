# Classes and Objects

## Micro Goal

Understand how to define classes as blueprints for objects, represent state with fields and properties, define behavior with methods, and create and use objects to model entities in a program.

## Concepts Covered

- Classes and objects
- Fields and properties
- Constructors and object initialization
- Instance methods and instance state
- The `this` keyword
- Encapsulation and access modifiers
- Auto-implemented properties
- Read-only properties
- Object initializers
- Reference types and object references
- Static members versus instance members
- `ToString()` and object representation
- Basic class design and responsibility

## Completion Criteria

- [ ] Define a class with fields, properties, and methods.
- [ ] Create multiple objects from the same class.
- [ ] Use a constructor to initialize object state.
- [ ] Explain the difference between a field and a property.
- [ ] Use access modifiers to control which members can be accessed.
- [ ] Explain how object references behave when assigned to another variable.
- [ ] Distinguish instance members from static members.
- [ ] Use object initializers and override `ToString()`.
- [ ] Model a Boid as an object with state and behavior.
- [ ] Complete the challenge using a clear, focused class design.

## Lecture

### 1. Classes and objects

A class defines a type that describes the data and behavior its instances will have. An object is an instance of a class.

For example, a Boids simulation may need many boids. Each boid has its own position, movement speed, and running state. Instead of keeping these values in unrelated variables, a class groups the state and related behavior together.

```csharp
class Boid
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Speed { get; set; }

    public void PrintPosition()
    {
        Console.WriteLine($"Position: ({X}, {Y})");
    }
}
```

Create objects using `new`:

```csharp
Boid firstBoid = new Boid();
firstBoid.X = 10.0;
firstBoid.Y = 20.0;
firstBoid.Speed = 2.0;

Boid secondBoid = new Boid();
secondBoid.X = 30.0;
secondBoid.Y = 40.0;
secondBoid.Speed = 1.5;

firstBoid.PrintPosition();
secondBoid.PrintPosition();
```

Both objects share the same class definition, but each has its own instance state. Changing `firstBoid.X` does not change `secondBoid.X`.

A class lets you model a concept as a cohesive unit rather than distributing its data and behavior across unrelated parts of a program.

### 2. Fields and properties

A **field** is a variable declared directly inside a class. It stores data belonging to the object or, if declared static, to the class itself.

A **property** provides a controlled way to read or write a value. Properties can expose a field, validate a new value, calculate a value, or restrict modification.

Consider this example:

```csharp
class Boid
{
    private double _speed;

    public double Speed
    {
        get { return _speed; }
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Speed cannot be negative.");
            }

            _speed = value;
        }
    }
}
```

The field `_speed` stores the actual value. The public `Speed` property controls access to it. The `value` keyword refers to the value supplied when the property is assigned.

```csharp
Boid boid = new Boid();
boid.Speed = 2.5;

Console.WriteLine(boid.Speed);
```

If code tries to assign a negative speed, the property rejects it by throwing an exception.

For simple properties, C# supports **auto-implemented properties**:

```csharp
public double X { get; set; }
public double Y { get; set; }
```

The compiler provides the backing storage. You do not need to declare fields manually unless you need additional logic or control.

Use fields primarily for internal implementation details. Use properties when exposing an object's state to other code. A private field and public property are a common combination when validation or other rules are required.

### 3. Constructors

A constructor initializes a new object. Its name matches the class name, and it has no return type.

```csharp
class Boid
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Speed { get; set; }

    public Boid(double x, double y, double speed)
    {
        X = x;
        Y = y;
        Speed = speed;
    }
}
```

Create an initialized object by supplying arguments:

```csharp
Boid boid = new Boid(10.0, 20.0, 2.5);
```

The constructor assigns the supplied values to the object's properties. This is useful when an object should have required initial data instead of being created in an incomplete state.

When you define no constructor, C# normally supplies a parameterless constructor for a class. When you declare a constructor yourself, the compiler does not automatically supply that parameterless constructor. If you need both forms, declare both explicitly.

For example:

```csharp
class Boid
{
    public double X { get; set; }
    public double Y { get; set; }

    public Boid()
    {
        X = 0.0;
        Y = 0.0;
    }

    public Boid(double x, double y)
    {
        X = x;
        Y = y;
    }
}
```

A constructor should establish the object's initial state. Avoid putting unrelated work, such as console menus or an entire simulation loop, inside a constructor.

### 4. The `this` keyword

The `this` keyword refers to the current instance. It can make it clear that a member belongs to the object when a parameter has the same name.

```csharp
class Boid
{
    public double Speed { get; private set; }

    public Boid(double speed)
    {
        this.Speed = speed;
    }
}
```

Here, `this.Speed` refers to the object's property, while `speed` refers to the constructor parameter.

The keyword is optional when there is no ambiguity, but it can improve clarity when distinguishing instance members from local variables or parameters.

### 5. Access modifiers and encapsulation

Access modifiers control where a member can be accessed.

- `public`: accessible to code that can access the containing type.
- `private`: accessible only within the containing type.
- `protected`: accessible within the containing type and its derived types.
- `internal`: accessible within the same assembly.

For this module, focus on `public` and `private`.

Encapsulation means keeping an object's internal implementation protected and exposing a clear interface for other code to use. This helps enforce rules about the object's state.

For example, a boid's speed should not become negative. A property can enforce that rule:

```csharp
class Boid
{
    private double _speed;

    public double Speed
    {
        get => _speed;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Speed cannot be negative.");
            }

            _speed = value;
        }
    }

    public Boid(double speed)
    {
        Speed = speed;
    }
}
```

The constructor uses the property to initialize the speed, so the same validation applies during construction and later assignments.

The constructor will throw if it receives a negative speed. This is preferable to allowing an invalid object to be created and hoping that later code handles it correctly.

### 6. Restricting property modification

A property can allow reading while restricting who may change it.

```csharp
class Boid
{
    public int Id { get; }
    public double X { get; private set; }
    public double Y { get; private set; }

    public Boid(int id, double x, double y)
    {
        Id = id;
        X = x;
        Y = y;
    }

    public void Move(double deltaX, double deltaY)
    {
        X += deltaX;
        Y += deltaY;
    }
}
```

`Id` has a getter but no setter. It can be assigned in the constructor and then read by other code, but ordinary external code cannot assign it a new value.

`X` and `Y` have private setters. Other code can read them, but only code inside `Boid` can assign them directly through those setters. The public `Move` method provides a controlled way to change the position.

```csharp
Boid boid = new Boid(1, 10.0, 20.0);

boid.Move(2.0, -1.0);

Console.WriteLine($"Position: ({boid.X}, {boid.Y})");
```

This design makes the object's public interface more meaningful. Code can request movement without directly managing the internal position updates.

### 7. Object initializers

An object initializer assigns properties as part of creating an object. It is convenient when the type has a parameterless constructor and does not require all state to be supplied through constructor arguments.

```csharp
class Boid
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Speed { get; set; }
}

Boid boid = new Boid
{
    X = 10.0,
    Y = 20.0,
    Speed = 2.5
};
```

The same initialization can use target-typed construction:

```csharp
Boid boid = new()
{
    X = 10.0,
    Y = 20.0,
    Speed = 2.5
};
```

Object initializers do not replace constructors. The constructor runs first, and then the initializer assigns the specified properties. If a class requires values to satisfy important invariants, a constructor with required parameters may be a better choice.

### 8. Objects are reference types

Classes are reference types. A variable of a class type holds a reference to an object, rather than storing the object's entire state directly in the variable.

Consider this example:

```csharp
Boid first = new Boid(1, 10.0, 20.0);
Boid second = first;

second.Move(5.0, 0.0);

Console.WriteLine(first.X);  // 15
Console.WriteLine(second.X); // 15
```

Both variables refer to the same object. Assigning `first` to `second` does not create a new Boid. Therefore, modifying the object through either reference is visible through the other.

To create an independent object, construct another instance:

```csharp
Boid first = new Boid(1, 10.0, 20.0);
Boid second = new Boid(2, 10.0, 20.0);

second.Move(5.0, 0.0);

Console.WriteLine(first.X);  // 10
Console.WriteLine(second.X); // 15
```

The two objects now have independent state.

This distinction is important when passing objects to methods or storing them in collections. For example, a `List<Boid>` stores references to Boid objects. Retrieving an element gives you a reference to the same object, so changing its properties changes that object.

### 9. Instance members and static members

An instance member belongs to an individual object. A static member belongs to the type itself.

For example, each boid has its own position and speed, so these are instance properties. A count of all boids created might instead be represented by a static member.

```csharp
class Boid
{
    private static int _nextId = 1;

    public int Id { get; }
    public double X { get; set; }
    public double Y { get; set; }

    public Boid(double x, double y)
    {
        Id = _nextId++;
        X = x;
        Y = y;
    }
}
```

Each instance receives its own `Id`, while `_nextId` is shared by the class. The next identifier is incremented whenever the constructor runs.

Create objects and access their instance properties normally:

```csharp
Boid first = new Boid(10.0, 20.0);
Boid second = new Boid(30.0, 40.0);

Console.WriteLine(first.Id);
Console.WriteLine(second.Id);
```

A static member is accessed through the type when it is accessible, rather than through a particular object. Static methods cannot directly access instance members because they do not have an implicit current instance.

Use static members for state or operations genuinely shared by the type. Do not make an object's state static just to make it easier to access; doing so would cause separate objects to share that state.

### 10. Object representation with `ToString()`

Every C# class ultimately inherits from `object`, which provides methods such as `ToString()`. The default implementation typically returns the type name, which is not always useful for debugging or displaying an object's state.

Override `ToString()` when a meaningful text representation is useful:

```csharp
class Boid
{
    public int Id { get; }
    public double X { get; private set; }
    public double Y { get; private set; }

    public Boid(int id, double x, double y)
    {
        Id = id;
        X = x;
        Y = y;
    }

    public override string ToString()
    {
        return $"Boid {Id} at ({X:F1}, {Y:F1})";
    }
}
```

Now you can write:

```csharp
Boid boid = new Boid(1, 10.0, 20.0);

Console.WriteLine(boid);
```

When an object is passed to `Console.WriteLine`, its string representation is used. Overriding `ToString()` gives you a concise way to inspect objects during development.

It is primarily a representation method, not a replacement for a structured data format or a dedicated logging system.

### 11. Designing a useful class

A class should represent a coherent concept and keep its related state and behavior together.

For a Boids simulation, a `Boid` class might own:

- Its identifier.
- Its position.
- Its current speed or velocity.
- Methods that update or report its state.

The simulation itself might be responsible for managing the collection of boids, advancing simulation steps, and coordinating the update process. A single `Boid` should not be responsible for managing every other boid or controlling the entire application.

A small example combines these ideas:

```csharp
class Boid
{
    public int Id { get; }
    public double X { get; private set; }
    public double Y { get; private set; }

    public Boid(int id, double x, double y)
    {
        Id = id;
        X = x;
        Y = y;
    }

    public void Move(double deltaX, double deltaY)
    {
        X += deltaX;
        Y += deltaY;
    }

    public override string ToString()
    {
        return $"Boid {Id}: ({X:F1}, {Y:F1})";
    }
}
```

This class has a clear responsibility: represent one boid, retain its identity and position, allow controlled movement, and provide a useful textual representation.

Classes become more valuable as programs grow because they establish boundaries around data and behavior. Those boundaries make it easier to test, extend, and reason about the program.

## Challenge

Create a small console program that models individual boids as objects.

Your program must satisfy all of the following requirements:

1. **Define a `Boid` class:** Include an integer identifier, X and Y coordinates, and a movement speed.
2. **Use a constructor:** Require the identifier, starting coordinates, and speed when creating a boid. Ensure the speed cannot be initialized to a negative value.
3. **Encapsulate state:** Make the identifier read-only after construction. Allow the position to be read publicly but modified only inside the class.
4. **Implement behavior:** Add a `Move` method that updates the position using X and Y offsets.
5. **Validate movement speed:** Expose speed through a property that rejects negative assignments.
6. **Override `ToString()`:** Return a readable description containing the boid's identifier, position, and speed.
7. **Create multiple objects:** Instantiate at least three boids with different identifiers, positions, and speeds.
8. **Demonstrate reference behavior:** Assign one boid reference to another variable and show that moving the object through one reference changes the state observed through the other.
9. **Demonstrate independent objects:** Move a separately constructed boid and show that the other boids remain unchanged.
10. **Use a collection:** Store the boids in a `List<Boid>` and print each object using `foreach`.
11. **Keep responsibilities clear:** Put boid-specific state and behavior in the `Boid` class. Keep the top-level statements focused on creating objects, invoking methods, and displaying results.

Use descriptive names, appropriate access modifiers, and straightforward logic. The program should demonstrate the difference between a class definition, an object instance, and a reference to an object.
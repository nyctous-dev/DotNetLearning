# Object-Oriented Programming

## Micro Goal

Understand the four core principles of object-oriented programming—encapsulation, abstraction, inheritance, and polymorphism—and apply them to build related C# types with clear responsibilities and reusable behavior.

## Concepts Covered

- Object-oriented programming principles
- Encapsulation and controlled access to state
- Abstraction and abstract classes
- Inheritance and derived classes
- Method overriding and `virtual` methods
- Abstract methods and properties
- Polymorphism and base-type references
- Compile-time versus runtime type
- `protected` access
- The `base` keyword
- Sealed classes and methods
- Composition versus inheritance
- When inheritance is appropriate

## Completion Criteria

- [ ] Explain encapsulation, abstraction, inheritance, and polymorphism.
- [ ] Use access modifiers to control access to class members.
- [ ] Create an abstract base class that defines a shared contract.
- [ ] Create derived classes that inherit from a base class.
- [ ] Override a virtual method and implement an abstract method.
- [ ] Demonstrate polymorphism using a collection of base-class references.
- [ ] Explain the difference between a variable's declared type and the runtime type of its object.
- [ ] Use `base` to invoke base-class behavior where appropriate.
- [ ] Identify when composition is preferable to inheritance.
- [ ] Build a small Boids-related example that uses object-oriented design without unnecessary abstraction.

## Lecture

### 1. What is object-oriented programming?

Object-oriented programming (OOP) organizes software around objects that combine state and behavior. A class defines a type, and an object is an instance of that type.

In the previous module, you created a `Boid` class with properties such as position and speed, plus a method for movement. That already used object-oriented programming: the boid's data and related behavior belonged together.

OOP becomes more useful when a program has multiple related types. For example, a simulation might have different kinds of entities that share some behavior but implement other behavior differently.

The four commonly recognized OOP principles are:

- **Encapsulation:** protect an object's state and control how it changes.
- **Abstraction:** expose essential behavior while hiding implementation details.
- **Inheritance:** define a specialized type based on an existing type.
- **Polymorphism:** use a shared type or contract to work with different implementations.

These principles are related, but they solve different problems. Inheritance does not automatically produce good encapsulation, and polymorphism does not require every type to use inheritance.

### 2. Encapsulation

Encapsulation means controlling access to an object's internal state and implementation. Instead of allowing arbitrary code to modify important data, a class exposes a controlled interface.

Consider a boid whose movement speed cannot be negative:

```csharp
class Boid
{
    private double _speed;

    public double Speed
    {
        get => _speed;
        private set
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

The field `_speed` is private. Code outside the class cannot access it directly. The `Speed` property exposes a read-only public view because its setter is private.

The constructor uses the property to initialize the speed, so the validation rule applies during construction as well.

Access modifiers help implement encapsulation:

- `public` members form part of the type's accessible interface.
- `private` members are accessible only inside the containing type.
- `protected` members are accessible inside the containing type and its derived types.
- `internal` members are accessible within the same assembly.

The goal is not to make everything private or everything public. The goal is to expose what other code needs and restrict access to implementation details that should remain controlled.

### 3. Abstraction

Abstraction means defining what a type can do without requiring callers to know how it does it.

For example, a simulation might need different entities to update themselves. The simulation should be able to request an update without knowing the internal algorithm of each entity.

An abstract class can define this contract:

```csharp
abstract class SimulationEntity
{
    public int Id { get; }

    protected SimulationEntity(int id)
    {
        Id = id;
    }

    public abstract void Update(double deltaTime);
}
```

The `abstract` keyword means that `SimulationEntity` is incomplete and cannot be instantiated directly.

The abstract method `Update` declares a required operation but provides no implementation. A concrete derived class must implement it.

The constructor is `protected`, so derived classes can initialize the inherited identifier, but ordinary external code cannot use that constructor to create a `SimulationEntity` directly.

Abstraction allows the simulation to work with the concept of an entity rather than depending on every implementation detail.

An abstract class can also contain implemented methods and properties. It can define shared behavior alongside operations that derived classes must implement.

### 4. Inheritance

Inheritance allows a class to derive from another class. The derived class inherits accessible members and can add or specialize behavior.

For example:

```csharp
class Boid : SimulationEntity
{
    public double X { get; private set; }
    public double Y { get; private set; }

    public Boid(int id, double x, double y)
        : base(id)
    {
        X = x;
        Y = y;
    }

    public override void Update(double deltaTime)
    {
        // Update this boid's state.
    }
}
```

The syntax `Boid : SimulationEntity` declares that `Boid` derives from `SimulationEntity`.

The constructor expression `: base(id)` calls the base-class constructor and initializes the inherited `Id` property.

The `Boid` class must implement `Update` because it inherits from an abstract class that declares this abstract method.

C# supports single class inheritance: a class can directly inherit from only one class. It can implement multiple interfaces, which provide another way to share contracts across otherwise unrelated types.

Inheritance should express a genuine *is-a* relationship. A boid can reasonably be a kind of simulation entity if it satisfies the entity's contract. A simulation itself is not a kind of boid, so making `Simulation` inherit from `Boid` would be a poor design.

### 5. Virtual methods and overriding

A base class can provide a default implementation that derived classes may replace. The `virtual` keyword permits overriding.

```csharp
class SimulationEntity
{
    public int Id { get; }

    public SimulationEntity(int id)
    {
        Id = id;
    }

    public virtual string Describe()
    {
        return $"Entity {Id}";
    }
}
```

A derived class can override that behavior:

```csharp
class Boid : SimulationEntity
{
    public double X { get; }
    public double Y { get; }

    public Boid(int id, double x, double y)
        : base(id)
    {
        X = x;
        Y = y;
    }

    public override string Describe()
    {
        return $"Boid {Id} at ({X:F1}, {Y:F1})";
    }
}
```

The `override` keyword indicates that the derived method replaces an inherited virtual or abstract member's implementation.

This differs from method overloading. **Overloading** defines methods with the same name but different parameter lists. **Overriding** provides a derived implementation of an inherited virtual or abstract member with a matching signature.

Use overriding when a derived type should provide specialized behavior through the same base-class contract.

### 6. Polymorphism

Polymorphism allows code to operate through a shared type while the actual object determines which overridden implementation runs.

Consider two different entities:

```csharp
class Obstacle : SimulationEntity
{
    public Obstacle(int id)
        : base(id)
    {
    }

    public override void Update(double deltaTime)
    {
        Console.WriteLine($"Obstacle {Id} remains stationary.");
    }
}
```

Assume the `Boid` class also implements `Update`. Both `Boid` and `Obstacle` are concrete subclasses of `SimulationEntity`.

You can store both in a collection of the base type:

```csharp
List<SimulationEntity> entities = new()
{
    new Boid(1, 10.0, 20.0),
    new Obstacle(2)
};

foreach (SimulationEntity entity in entities)
{
    entity.Update(1.0);
}
```

The variable `entity` has the declared type `SimulationEntity`, but each object has its own runtime type. When `Update` is called, C# dispatches to the appropriate override for that object.

The collection does not need to know how a boid updates its position or how an obstacle behaves. It only needs to know that each entity supports `Update`.

That is the practical value of polymorphism: one piece of code can work with different implementations through a common contract.

### 7. Declared type and runtime type

A variable's declared type determines which members are accessible through that variable at compile time. The object's runtime type determines which implementation of a virtual member is invoked.

For example:

```csharp
SimulationEntity entity = new Boid(1, 10.0, 20.0);
```

The variable `entity` is declared as `SimulationEntity`, but it refers to a `Boid` object.

You can call members declared by `SimulationEntity`:

```csharp
entity.Update(1.0);
```

If `Update` is overridden in `Boid`, the `Boid` implementation executes.

However, you cannot directly access a `Boid`-specific property through the `SimulationEntity` variable unless that property is also declared on the base type.

This separation lets code depend on the shared contract instead of every implementation's specific details.

You can inspect the runtime type with `GetType()`:

```csharp
Console.WriteLine(entity.GetType().Name);
```

This prints `Boid` for the object above. Runtime type inspection can be useful for debugging, but well-designed polymorphic code generally relies on virtual methods rather than repeatedly checking types.

### 8. The `base` keyword

The `base` keyword lets a derived class access an inherited implementation or invoke a base-class constructor.

You have already seen constructor chaining:

```csharp
public Boid(int id)
    : base(id)
{
}
```

You can also call an overridden base method:

```csharp
class SimulationEntity
{
    public int Id { get; }

    public SimulationEntity(int id)
    {
        Id = id;
    }

    public virtual string Describe()
    {
        return $"Entity {Id}";
    }
}

class Boid : SimulationEntity
{
    public Boid(int id)
        : base(id)
    {
    }

    public override string Describe()
    {
        return $"{base.Describe()} is a boid";
    }
}
```

Calling `base.Describe()` reuses the base implementation and extends its result. It does not invoke the override recursively.

Use `base` when the inherited implementation is useful to the derived type. Do not call it automatically in every override; sometimes the derived implementation should replace the base behavior entirely.

### 9. Abstract members versus virtual members

Abstract and virtual members both support polymorphism, but their requirements differ.

An **abstract member** declares behavior that a concrete derived class must implement. It has no implementation in the abstract base class.

A **virtual member** provides a default implementation that a derived class may override.

```csharp
abstract class SimulationEntity
{
    public int Id { get; }

    protected SimulationEntity(int id)
    {
        Id = id;
    }

    public abstract void Update(double deltaTime);

    public virtual string Describe()
    {
        return $"Entity {Id}";
    }
}
```

Here, every concrete entity must implement `Update`, while `Describe` can be inherited unchanged or overridden.

Use abstract members when the base class cannot provide a meaningful general implementation and every concrete subtype must supply one. Use virtual members when a sensible default exists but specialization is allowed.

### 10. Sealed classes and methods

The `sealed` keyword prevents further inheritance or overriding.

A sealed class cannot be used as a base class:

```csharp
sealed class Boid
{
}
```

A sealed override prevents a further-derived class from overriding that particular implementation:

```csharp
class SimulationEntity
{
    public virtual void Update()
    {
    }
}

class Boid : SimulationEntity
{
    public sealed override void Update()
    {
    }
}
```

Use `sealed` when the design intentionally prevents extension at that level. Do not add it simply as decoration; it should express a real restriction in the type hierarchy.

### 11. Composition versus inheritance

Inheritance is useful when one type is genuinely a specialized form of another. However, it is not the only way to reuse behavior.

**Composition** means a class contains or uses other objects to perform its work.

For example, a boid may use a separate movement component:

```csharp
class MovementController
{
    public double Speed { get; set; }

    public MovementController(double speed)
    {
        Speed = speed;
    }
}

class Boid
{
    public MovementController Movement { get; }

    public Boid(double speed)
    {
        Movement = new MovementController(speed);
    }
}
```

A `Boid` *has a* `MovementController`; it is not a kind of movement controller.

Composition is often a better fit when behavior can be reused across unrelated classes, when components should be replaceable, or when inheritance would create a deep and rigid hierarchy.

Inheritance is appropriate when derived objects can legitimately be treated as instances of the base type and honor its contract. Composition is often preferable when one object merely needs the services or functionality of another.

Do not create a large inheritance hierarchy just to avoid repeating a few lines of code. A small amount of duplication can be simpler than an abstraction that does not represent a genuine relationship.

### 12. Applying OOP to the Boids simulation

A Boids simulation can use these principles without creating unnecessary classes.

A reasonable initial design might include:

- `SimulationEntity` as an abstract base type if multiple kinds of entities share an update contract.
- `Boid` as a concrete type containing position, velocity, and boid-specific behavior.
- `Obstacle` as another entity if the simulation needs obstacles that participate in the same update system.
- `Simulation` as the coordinator that stores and updates entities.

Not every project needs all these types. If the application only has boids, a single `Boid` class and a simulation coordinator may be enough. An abstract base class is useful only when there is a real shared contract or a meaningful need for polymorphism.

The important objective is to give each class a clear responsibility and use OOP features to solve actual design problems.

## Challenge

Build a console program that demonstrates encapsulation, abstraction, inheritance, and polymorphism using a small simulation-entity model.

Your program must satisfy all of the following requirements:

1. **Create an abstract base class:** Define `SimulationEntity` with a read-only integer identifier, a constructor, an abstract `Update(double deltaTime)` method, and a virtual `Describe()` method.
2. **Create a `Boid` class:** Derive it from `SimulationEntity`. Store X and Y coordinates, provide controlled access to position, and implement `Update` by moving the boid using its velocity and the supplied time delta.
3. **Create an `Obstacle` class:** Derive it from `SimulationEntity`. Implement `Update` with behavior appropriate for a stationary obstacle, and override `Describe()` to identify the object as an obstacle.
4. **Demonstrate encapsulation:** Prevent external code from directly setting a boid's position. Keep state changes within its methods.
5. **Validate inputs:** Reject negative or non-finite time deltas. Reject invalid boid speeds or velocity components rather than allowing invalid numeric state into the simulation.
6. **Demonstrate inheritance:** Use the base-class constructor from both derived classes.
7. **Demonstrate polymorphism:** Store at least two boids and one obstacle in a `List<SimulationEntity>`. Iterate through the list, call `Update`, and print each entity's description.
8. **Demonstrate overriding:** Ensure the output from `Describe()` differs appropriately between boids and obstacles.
9. **Use `base` appropriately:** Reuse the base implementation of `Describe()` in at least one derived class, extending its output rather than duplicating the base description.
10. **Keep the design focused:** Put entity-specific state and behavior in the relevant classes. Keep the top-level statements focused on creating the entities, updating them, and displaying the results.

Use straightforward code and clear names. Avoid type checks such as `if (entity is Boid)` to decide which update method to call; the point of the exercise is to let polymorphism select the appropriate implementation.
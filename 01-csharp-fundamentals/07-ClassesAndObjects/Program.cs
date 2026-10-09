
List<Boid> boids = new()
{
    new Boid(1, 10.0, 20.0, 1.5),
    new Boid(2, 30.0, 40.0, 2.0),
    new Boid(3, 50.0, 60.0, 2.5)
};

Console.WriteLine("=== Initial Boids ===");

foreach (Boid boid in boids)
{
    Console.WriteLine(boid);
}

// Both variables reference the same object.
Boid originalReference = boids[0];
Boid sharedReference = originalReference;

sharedReference.Move(5.0, -2.0);

Console.WriteLine("\n=== Shared Reference ===");
Console.WriteLine($"Original reference: {originalReference}");
Console.WriteLine($"Shared reference:   {sharedReference}");
Console.WriteLine(
    $"Same object: {ReferenceEquals(originalReference, sharedReference)}");

// This is a separate object with independent state.
Boid independentBoid = new(4, 10.0, 20.0, 3.0);
independentBoid.Move(-3.0, 4.0);

Console.WriteLine("\n=== Independent Object ===");
Console.WriteLine($"Independent boid: {independentBoid}");
Console.WriteLine($"Original boid:    {originalReference}");

Console.WriteLine("\n=== All Boids in the List ===");

foreach (Boid boid in boids)
{
    Console.WriteLine(boid);
}

sealed class Boid
{
    public int Id { get; }

    public double X { get; private set; }

    public double Y { get; private set; }

    private double _speed;

    public double Speed
    {
        get => _speed;
        set
        {
            if (value < 0 || !double.IsFinite(value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Speed must be finite and non-negative.");
            }

            _speed = value;
        }
    }

    public Boid(int id, double x, double y, double speed)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                "ID must be positive.");
        }

        if (!double.IsFinite(x) || !double.IsFinite(y))
        {
            throw new ArgumentOutOfRangeException(
                nameof(x),
                "Coordinates must be finite.");
        }

        Id = id;
        X = x;
        Y = y;
        Speed = speed;
    }

    public void Move(double deltaX, double deltaY)
    {
        if (!double.IsFinite(deltaX) || !double.IsFinite(deltaY))
        {
            throw new ArgumentOutOfRangeException(
                nameof(deltaX),
                "Movement offsets must be finite.");
        }

        X += deltaX;
        Y += deltaY;
    }

    public override string ToString()
    {
        return $"Boid {Id}: Position=({X:F1}, {Y:F1}), Speed={Speed:F1}";
    }
}

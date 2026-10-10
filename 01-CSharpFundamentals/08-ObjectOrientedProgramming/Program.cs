
List<SimulationEntity> entities =
[
    new Boid(1, 10.0, 20.0, 2.0, 1.0),
    new Boid(2, 5.0, 8.0, -1.0, 3.0),
    new Obstacle(3)
];

const double deltaTime = 1.0;

foreach (SimulationEntity entity in entities)
{
    entity.Update(deltaTime);
    Console.WriteLine(entity.Describe());
}

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

    protected static void ValidateDeltaTime(double deltaTime)
    {
        if (!double.IsFinite(deltaTime) || deltaTime < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(deltaTime),
                "Delta time must be finite and non-negative.");
        }
    }
}

class Boid : SimulationEntity
{
    public double X { get; private set; }
    public double Y { get; private set; }

    public double VelocityX { get; }
    public double VelocityY { get; }
    public double Speed { get; }

    public Boid(
        int id,
        double x,
        double y,
        double velocityX,
        double velocityY)
        : base(id)
    {
        if (!double.IsFinite(x))
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        if (!double.IsFinite(y))
        {
            throw new ArgumentOutOfRangeException(nameof(y));
        }

        if (!double.IsFinite(velocityX))
        {
            throw new ArgumentOutOfRangeException(nameof(velocityX));
        }

        if (!double.IsFinite(velocityY))
        {
            throw new ArgumentOutOfRangeException(nameof(velocityY));
        }

        double speed = Math.Sqrt(
            velocityX * velocityX + velocityY * velocityY);

        if (!double.IsFinite(speed))
        {
            throw new ArgumentException(
                "Velocity components must produce a finite speed.");
        }

        X = x;
        Y = y;
        VelocityX = velocityX;
        VelocityY = velocityY;
        Speed = speed;
    }

    public override void Update(double deltaTime)
    {
        ValidateDeltaTime(deltaTime);

        double nextX = X + VelocityX * deltaTime;
        double nextY = Y + VelocityY * deltaTime;

        if (!double.IsFinite(nextX) || !double.IsFinite(nextY))
        {
            throw new InvalidOperationException(
                $"Updating boid {Id} would produce an invalid position.");
        }

        X = nextX;
        Y = nextY;
    }

    public override string Describe()
    {
        return $"{base.Describe()} is a boid at ({X:F1}, {Y:F1}) " +
               $"with speed {Speed:F1}.";
    }
}

class Obstacle : SimulationEntity
{
    public Obstacle(int id) : base(id)
    {
    }

    public override void Update(double deltaTime)
    {
        ValidateDeltaTime(deltaTime);
    }

    public override string Describe()
    {
        return $"{base.Describe()} is a stationary obstacle.";
    }
}

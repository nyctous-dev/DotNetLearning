namespace BoidLogic;

public sealed class Boid
{
    public double X { get; private set; }
    public double Y { get; private set; }
    public double Speed { get; }

    public Boid(int id, double x, double y, double speed)
    {
        if (speed < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(speed),
                "Speed cannot be negative.");
        }

        Id = id;
        X = x;
        Y = y;
        Speed = speed;
    }

    public int Id { get; }

    public void Move(double offsetX, double offsetY)
    {
        X += offsetX;
        Y += offsetY;
    }
}
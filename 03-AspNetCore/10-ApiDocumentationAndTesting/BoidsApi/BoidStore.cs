public sealed record Boid(int Id, double X, double Y, double Speed);

public sealed record CreateBoidRequest(double X, double Y, double Speed);

public sealed record ApiError(string Error);

public sealed class BoidStore
{
    private readonly List<Boid> _boids =
    [
        new(1, 10.0, 20.0, 2.0),
        new(2, 30.0, 40.0, 3.0)
    ];

    private int _nextId = 3;

    public Boid[] GetAll()
    {
        lock (_boids)
        {
            return _boids.ToArray();
        }
    }

    public Boid? GetById(int id)
    {
        lock (_boids)
        {
            return _boids.FirstOrDefault(boid => boid.Id == id);
        }
    }

    public Boid Create(CreateBoidRequest request)
    {
        lock (_boids)
        {
            Boid boid = new(
                _nextId++,
                request.X,
                request.Y,
                request.Speed);

            _boids.Add(boid);
            return boid;
        }
    }
}
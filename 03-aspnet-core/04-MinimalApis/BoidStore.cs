public sealed class BoidStore
{
    private readonly Dictionary<int, Boid> _boids = [];
    private readonly object _lock = new();
    private int _nextId = 1;

    public IReadOnlyList<Boid> GetAll()
    {
        lock (_lock)
        {
            return _boids.Values
                .OrderBy(boid => boid.Id)
                .ToArray();
        }
    }

    public Boid? GetById(int id)
    {
        lock (_lock)
        {
            return _boids.GetValueOrDefault(id);
        }
    }

    public Boid Create(CreateBoidRequest request)
    {
        lock (_lock)
        {
            Boid boid = new(
                _nextId++,
                request.X,
                request.Y,
                request.Speed);

            _boids.Add(boid.Id, boid);
            return boid;
        }
    }

    public bool Replace(int id, ReplaceBoidRequest request)
    {
        lock (_lock)
        {
            if (!_boids.ContainsKey(id))
            {
                return false;
            }

            _boids[id] = new(id, request.X, request.Y, request.Speed);
            return true;
        }
    }

    public bool Patch(int id, PatchBoidRequest request)
    {
        lock (_lock)
        {
            if (!_boids.TryGetValue(id, out Boid? current))
            {
                return false;
            }

            _boids[id] = current with
            {
                X = request.X ?? current.X,
                Y = request.Y ?? current.Y,
                Speed = request.Speed ?? current.Speed
            };

            return true;
        }
    }

    public bool Delete(int id)
    {
        lock (_lock)
        {
            return _boids.Remove(id);
        }
    }
}
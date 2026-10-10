public sealed record Boid(
    int Id,
    double X,
    double Y,
    double Speed,
    bool Active);

public sealed record CreateBoidRequest(
    double X,
    double Y,
    double Speed,
    bool Active = true);
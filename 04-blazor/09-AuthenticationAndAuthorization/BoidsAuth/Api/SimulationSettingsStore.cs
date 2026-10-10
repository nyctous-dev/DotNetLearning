namespace BoidsAuth.Api;

public sealed record SimulationSettings(
    int BoidCount,
    double MovementSpeed);

public sealed record UpdateSimulationSettingsRequest(
    int BoidCount,
    double MovementSpeed);

public sealed record ApiError(string Error);

public sealed class SimulationSettingsStore
{
    private SimulationSettings _current = new(
        BoidCount: 100,
        MovementSpeed: 2.0);

    public SimulationSettings Current => _current;

    public SimulationSettings Update(
        UpdateSimulationSettingsRequest request)
    {
        _current = new SimulationSettings(
            request.BoidCount,
            request.MovementSpeed);

        return _current;
    }
}
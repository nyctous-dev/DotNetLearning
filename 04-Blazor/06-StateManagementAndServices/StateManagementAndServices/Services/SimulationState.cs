namespace StateManagementAndServices.Services;

public sealed class SimulationState
{
    public event Action? OnChange;

    public bool IsRunning { get; private set; }

    public int BoidCount { get; private set; } = 100;

    public void SetRunning(bool isRunning)
    {
        if (IsRunning == isRunning)
        {
            return;
        }

        IsRunning = isRunning;
        NotifyStateChanged();
    }

    public void SetBoidCount(int boidCount)
    {
        if (boidCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(boidCount),
                "Boid count cannot be negative.");
        }

        if (BoidCount == boidCount)
        {
            return;
        }

        BoidCount = boidCount;
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}
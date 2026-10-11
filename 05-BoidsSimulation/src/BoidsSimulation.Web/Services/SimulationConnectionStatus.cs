namespace BoidsSimulation.Web.Services;

public sealed class SimulationConnectionStatus
{
    public string Status { get; private set; } = "Connecting";

    public bool IsConnected =>
        string.Equals(Status, "Connected", StringComparison.Ordinal);

    public event Action? Changed;

    public void Update(string status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        var newStatus = status switch
        {
            "Connected" => "Connected",
            "Connecting" => "Connecting",
            "Reconnecting" => "Reconnecting",
            _ => "Disconnected"
        };

        if (Status == newStatus)
        {
            return;
        }

        Status = newStatus;
        Changed?.Invoke();
    }
}
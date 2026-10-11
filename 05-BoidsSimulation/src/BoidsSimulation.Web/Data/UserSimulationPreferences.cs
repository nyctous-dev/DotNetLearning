
namespace BoidsSimulation.Web.Data;

public sealed class UserSimulationPreferences
{
    public string UserId { get; set; } = string.Empty;

    public string BoidColor { get; set; } = "#36D9F5";

    public string BackgroundColor { get; set; } = "#0A111A";

    public bool ShowTrails { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

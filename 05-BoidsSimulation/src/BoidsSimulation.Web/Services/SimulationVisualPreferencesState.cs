namespace BoidsSimulation.Web.Services;

public sealed record SimulationVisualPreferences(
    string BoidColor,
    string BackgroundColor,
    bool ShowTrails)
{
    public static SimulationVisualPreferences Default { get; } = new(
        BoidColor: "#36D9F5",
        BackgroundColor: "#0A111A",
        ShowTrails: false);
}

public sealed class SimulationVisualPreferencesState
{
    public SimulationVisualPreferences Current { get; private set; } =
        SimulationVisualPreferences.Default;

    public event Action? Changed;

    public void SetBoidColor(string? color)
    {
        if (!IsHexColor(color))
        {
            return;
        }

        Update(Current with { BoidColor = color!.ToUpperInvariant() });
    }

    public void SetBackgroundColor(string? color)
    {
        if (!IsHexColor(color))
        {
            return;
        }

        Update(Current with { BackgroundColor = color!.ToUpperInvariant() });
    }

    public void SetShowTrails(bool showTrails)
    {
        Update(Current with { ShowTrails = showTrails });
    }

    private void Update(SimulationVisualPreferences updated)
    {
        if (Current == updated)
        {
            return;
        }

        Current = updated;
        Changed?.Invoke();
    }

    private static bool IsHexColor(string? color)
    {
        if (color is not { Length: 7 } || color[0] != '#')
        {
            return false;
        }

        for (var i = 1; i < color.Length; i++)
        {
            if (!Uri.IsHexDigit(color[i]))
            {
                return false;
            }
        }

        return true;
    }
}
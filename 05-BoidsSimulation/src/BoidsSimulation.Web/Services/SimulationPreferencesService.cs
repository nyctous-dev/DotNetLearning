using System.Security.Claims;
using BoidsSimulation.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoidsSimulation.Web.Services;

public sealed record SimulationPreferences(
    string BoidColor,
    string BackgroundColor,
    bool ShowTrails)
{
    public static SimulationPreferences Default { get; } = new(
        BoidColor: "#36D9F5",
        BackgroundColor: "#0A111A",
        ShowTrails: false);
}

public sealed class SimulationPreferencesService(
    ApplicationDbContext dbContext,
    UserManager<IdentityUser> userManager)
{
    public async Task<SimulationPreferences> GetAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var user = await GetAuthenticatedUserAsync(principal);

        var savedPreferences = await dbContext.UserSimulationPreferences
            .AsNoTracking()
            .SingleOrDefaultAsync(
                preferences => preferences.UserId == user.Id,
                cancellationToken);

        if (savedPreferences is null)
        {
            return SimulationPreferences.Default;
        }

        return new SimulationPreferences(
            savedPreferences.BoidColor,
            savedPreferences.BackgroundColor,
            savedPreferences.ShowTrails);
    }

    public async Task<SimulationPreferences> SaveAsync(
        ClaimsPrincipal principal,
        SimulationPreferences preferences,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(preferences);

        if (!IsHexColor(preferences.BoidColor))
        {
            throw new ArgumentException(
                "BoidColor must be a hexadecimal color such as #36D9F5.",
                nameof(preferences));
        }

        if (!IsHexColor(preferences.BackgroundColor))
        {
            throw new ArgumentException(
                "BackgroundColor must be a hexadecimal color such as #0A111A.",
                nameof(preferences));
        }

        var user = await GetAuthenticatedUserAsync(principal);

        var savedPreferences = await dbContext.UserSimulationPreferences
            .SingleOrDefaultAsync(
                item => item.UserId == user.Id,
                cancellationToken);

        if (savedPreferences is null)
        {
            savedPreferences = new UserSimulationPreferences
            {
                UserId = user.Id
            };

            dbContext.UserSimulationPreferences.Add(savedPreferences);
        }

        savedPreferences.BoidColor =
            preferences.BoidColor.ToUpperInvariant();

        savedPreferences.BackgroundColor =
            preferences.BackgroundColor.ToUpperInvariant();

        savedPreferences.ShowTrails = preferences.ShowTrails;
        savedPreferences.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new SimulationPreferences(
            savedPreferences.BoidColor,
            savedPreferences.BackgroundColor,
            savedPreferences.ShowTrails);
    }

    private async Task<IdentityUser> GetAuthenticatedUserAsync(
        ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException(
                "An authenticated user is required to access saved preferences.");
        }

        return await userManager.GetUserAsync(principal)
            ?? throw new UnauthorizedAccessException(
                "The authenticated account could not be found.");
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
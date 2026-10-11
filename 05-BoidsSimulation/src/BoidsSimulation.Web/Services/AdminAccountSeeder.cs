using Microsoft.AspNetCore.Identity;

namespace BoidsSimulation.Web.Services;

public sealed class AdminAccountSeeder(
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IConfiguration configuration,
    ILogger<AdminAccountSeeder> logger)
{
    private const string AdminRoleName = "Admin";

    public async Task SeedAsync()
    {
        // Ensure the Admin role exists.
        var role = await roleManager.FindByNameAsync(AdminRoleName);

        if (role is null)
        {
            var roleResult = await roleManager.CreateAsync(
                new IdentityRole(AdminRoleName));

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not create the Admin role: " +
                    FormatErrors(roleResult));
            }

            logger.LogInformation(
                "Created the {RoleName} role.",
                AdminRoleName);
        }

        // Bootstrap credentials must be provided together.
        var email = configuration["AdminBootstrap:Email"]?.Trim();
        var password = configuration["AdminBootstrap:Password"];

        var hasEmail = !string.IsNullOrWhiteSpace(email);
        var hasPassword = !string.IsNullOrEmpty(password);

        if (hasEmail != hasPassword)
        {
            throw new InvalidOperationException(
                "Configure both AdminBootstrap:Email and " +
                "AdminBootstrap:Password, or leave both unset.");
        }

        // The role can exist without automatically creating an account.
        if (!hasEmail)
        {
            logger.LogInformation(
                "No administrator account was bootstrapped. " +
                "AdminBootstrap credentials are not configured.");

            return;
        }

        var user = await userManager.FindByEmailAsync(email!);
        var createdBySeeder = false;

        if (user is null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var userResult = await userManager.CreateAsync(
                user,
                password!);

            if (!userResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not create the bootstrap administrator: " +
                    FormatErrors(userResult));
            }

            createdBySeeder = true;
        }

        // Never grant Admin to an existing account just because
        // its email matches the bootstrap configuration.
        if (!createdBySeeder)
        {
            if (await userManager.IsInRoleAsync(user, AdminRoleName))
            {
                logger.LogInformation(
                    "The configured administrator account already exists.");

                return;
            }

            throw new InvalidOperationException(
                "The configured bootstrap email belongs to an existing " +
                "non-admin account. Refusing to grant administrator access. " +
                "Use a new, dedicated administrator email.");
        }

        var addToRoleResult = await userManager.AddToRoleAsync(
            user,
            AdminRoleName);

        if (!addToRoleResult.Succeeded)
        {
            // Avoid leaving behind a partially bootstrapped account.
            var deleteResult = await userManager.DeleteAsync(user);

            var cleanupMessage = deleteResult.Succeeded
                ? string.Empty
                : " Cleanup also failed: " + FormatErrors(deleteResult);

            throw new InvalidOperationException(
                "Could not assign the Admin role: " +
                FormatErrors(addToRoleResult) +
                cleanupMessage);
        }

        logger.LogInformation(
            "Created the bootstrap administrator account.");
    }

    private static string FormatErrors(IdentityResult result)
    {
        return string.Join(
            "; ",
            result.Errors.Select(error =>
                $"{error.Code}: {error.Description}"));
    }
}
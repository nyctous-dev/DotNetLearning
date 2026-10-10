
using BoidsAuth.Api;
using BoidsAuth.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRazorPages();
builder.Services.AddCascadingAuthenticationState();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/forbidden";

        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy =
            builder.Environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;

        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanManageSimulations", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Administrator");
    });
});

builder.Services
    .AddSingleton<IPasswordHasher<LocalUser>, PasswordHasher<LocalUser>>();

builder.Services.AddSingleton(serviceProvider =>
    new LocalUserStore(
        Path.Combine(
            builder.Environment.ContentRootPath,
            "data",
            "users.json"),
        serviceProvider.GetRequiredService<
            IPasswordHasher<LocalUser>>()));

builder.Services.AddSingleton<SimulationSettingsStore>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorPages();

app.MapGet(
    "/api/simulation/settings",
    (SimulationSettingsStore store) =>
        Results.Ok(store.Current))
    .WithSummary("Get simulation settings")
    .RequireAuthorization();

app.MapPost(
    "/api/simulation/settings",
    (UpdateSimulationSettingsRequest request,
     SimulationSettingsStore store) =>
    {
        if (request.BoidCount < 0 ||
            !double.IsFinite(request.MovementSpeed) ||
            request.MovementSpeed < 0)
        {
            return Results.BadRequest(
                new ApiError(
                    "Boid count must be non-negative and movement speed must be finite and non-negative."));
        }

        return Results.Ok(store.Update(request));
    })
    .WithSummary("Update simulation settings")
    .RequireAuthorization("CanManageSimulations");

app.MapRazorComponents<BoidsAuth.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program { }

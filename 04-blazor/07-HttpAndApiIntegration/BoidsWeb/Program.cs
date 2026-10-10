using BoidsWeb.Components;
using BoidsWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

string apiBaseUrl =
    builder.Configuration["BoidsApi:BaseUrl"]
    ?? throw new InvalidOperationException(
        "BoidsApi:BaseUrl is not configured.");

builder.Services.AddHttpClient<BoidsApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
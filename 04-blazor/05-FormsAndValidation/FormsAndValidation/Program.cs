using FormsAndValidation.Components;
using System.ComponentModel.DataAnnotations;
using FormsAndValidation.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();



app.MapPost("/api/simulation-settings", (SimulationSettings settings) =>
{
    var validationResults = new List<ValidationResult>();

    var context = new ValidationContext(settings);

    bool isValid = Validator.TryValidateObject(
        settings,
        context,
        validationResults,
        validateAllProperties: true);

    // IValidatableObject rules, including the custom rule,
    // are evaluated by TryValidateObject.
    if (!isValid)
    {
        return Results.ValidationProblem(
            validationResults
                .SelectMany(result =>
                    (result.MemberNames.Any()
                        ? result.MemberNames
                        : [string.Empty])
                    .Select(member => new
                    {
                        Member = member,
                        Message = result.ErrorMessage ?? "Invalid value."
                    }))
                .GroupBy(item => item.Member)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(item => item.Message).ToArray()));
    }

    return Results.Ok(new
    {
        Message = "Simulation settings validated successfully.",
        settings.Name,
        settings.BoidCount,
        settings.MovementSpeed
    });
})
.WithName("ValidateSimulationSettings");

app.Run();

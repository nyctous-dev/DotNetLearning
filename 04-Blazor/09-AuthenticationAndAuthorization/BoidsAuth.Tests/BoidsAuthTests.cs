
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using BoidsAuth.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

public sealed class BoidsAuthFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = TestAuthHandler.Scheme;
                    options.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                    options.DefaultChallengeScheme = TestAuthHandler.Scheme;
                    options.DefaultForbidScheme = TestAuthHandler.Scheme;
                })
                .AddScheme<
                    AuthenticationSchemeOptions,
                    TestAuthHandler>(
                    TestAuthHandler.Scheme,
                    _ => { });
        });
    }
}

public sealed class TestAuthHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Scheme = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var username))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, username.ToString()),
            new(ClaimTypes.Name, username.ToString())
        ];

        if (Request.Headers.TryGetValue("X-Test-Role", out var role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
        }

        ClaimsIdentity identity = new(claims, Scheme);
        ClaimsPrincipal principal = new(identity);

        AuthenticationTicket ticket = new(principal, Scheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public sealed class BoidsAuthTests
{
    [Fact]
    public async Task UnauthenticatedRequest_ReturnsUnauthorized()
    {
        using BoidsAuthFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response =
            await client.GetAsync("/api/simulation/settings");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedUser_CanReadSettings()
    {
        using BoidsAuthFactory factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpRequestMessage request = new(
            HttpMethod.Get,
            "/api/simulation/settings");

        request.Headers.Add("X-Test-User", "alice");
        request.Headers.Add("X-Test-Role", "Viewer");

        using HttpResponseMessage response =
            await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SimulationSettings? settings =
            await response.Content.ReadFromJsonAsync<SimulationSettings>();

        Assert.NotNull(settings);
        Assert.Equal(100, settings.BoidCount);
        Assert.Equal(2.0, settings.MovementSpeed);
    }

    [Fact]
    public async Task Viewer_CannotUpdateSettings()
    {
        using BoidsAuthFactory factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpRequestMessage request = new(
            HttpMethod.Post,
            "/api/simulation/settings")
        {
            Content = JsonContent.Create(
                new UpdateSimulationSettingsRequest(75, 1.5))
        };

        request.Headers.Add("X-Test-User", "alice");
        request.Headers.Add("X-Test-Role", "Viewer");

        using HttpResponseMessage response =
            await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Administrator_CanUpdateSettings()
    {
        using BoidsAuthFactory factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpRequestMessage request = new(
            HttpMethod.Post,
            "/api/simulation/settings")
        {
            Content = JsonContent.Create(
                new UpdateSimulationSettingsRequest(75, 1.5))
        };

        request.Headers.Add("X-Test-User", "admin");
        request.Headers.Add("X-Test-Role", "Administrator");

        using HttpResponseMessage response =
            await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SimulationSettings? settings =
            await response.Content.ReadFromJsonAsync<SimulationSettings>();

        Assert.NotNull(settings);
        Assert.Equal(75, settings.BoidCount);
        Assert.Equal(1.5, settings.MovementSpeed);
    }
}

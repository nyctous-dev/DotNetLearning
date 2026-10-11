using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace BoidsSimulation.Web.Tests;

public sealed class BoidsSimulationWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"boids-simulation-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:IdentityConnection"] =
                        $"Data Source={_databasePath}"
                });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        TryDeleteDatabaseFile(_databasePath);
        TryDeleteDatabaseFile(_databasePath + "-shm");
        TryDeleteDatabaseFile(_databasePath + "-wal");
    }

    private static void TryDeleteDatabaseFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // A temporary test database should not prevent test cleanup.
        }
        catch (UnauthorizedAccessException)
        {
            // A temporary test database should not prevent test cleanup.
        }
    }
}
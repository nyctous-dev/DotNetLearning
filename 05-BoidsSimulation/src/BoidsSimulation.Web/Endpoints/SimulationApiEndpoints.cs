
using System.Globalization;
using BoidsSimulation.Core.Models;
using BoidsSimulation.Core.Simulation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BoidsSimulation.Web.Endpoints;

public static class SimulationApiEndpoints
{
    public static IEndpointRouteBuilder MapSimulationApi(
        this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api");

        api.MapGet("/simulation", GetSimulationSummary);
        api.MapGet("/boids/{id}", GetBoidById);

        return endpoints;
    }

    private static IResult GetSimulationSummary(
        SimulationState state,
        SimulationSeedProvider seedProvider,
        SimulationSettings settings)
    {
        return Results.Ok(new
        {
            boidCount = state.BoidCount,
            seed = seedProvider.Seed,
            worldWidth = settings.WorldWidth,
            worldHeight = settings.WorldHeight
        });
    }

    private static IResult GetBoidById(
        string id,
        SimulationState state)
    {
        if (!int.TryParse(
                id,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var boidId))
        {
            return Results.BadRequest(new
            {
                error = "Boid ID must be a valid integer."
            });
        }

        if (!state.TryGetBoid(boidId, out var boid) || boid is null)
        {
            return Results.NotFound(new
            {
                error = $"Boid with ID {boidId} was not found."
            });
        }

        return Results.Ok(new
        {
            id = boid.Id,
            position = new
            {
                x = boid.Position.X,
                y = boid.Position.Y
            },
            velocity = new
            {
                x = boid.Velocity.X,
                y = boid.Velocity.Y
            }
        });
    }
}

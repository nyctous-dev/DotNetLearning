using DataTransferObjectsAndValidation.Dtos;
using DataTransferObjectsAndValidation.Models;
using DataTransferObjectsAndValidation.Services;
using DataTransferObjectsAndValidation.Validation;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SimulationStore>();

WebApplication app = builder.Build();

app.MapPost(
    "/simulations",
    (CreateSimulationRequest request, SimulationStore store) =>
    {
        Dictionary<string, string[]> errors =
            RequestValidator.Validate(request);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        Simulation simulation = new()
        {
            Name = request.Name,
            BoidCount = request.InitialBoidCount,
            IsRunning = false
        };

        store.Add(simulation);

        SimulationResponse response = new()
        {
            Id = simulation.Id,
            Name = simulation.Name,
            BoidCount = simulation.BoidCount,
            IsRunning = simulation.IsRunning
        };

        return Results.Created(
            $"/simulations/{simulation.Id}",
            response);
    });

app.MapGet(
    "/simulations/{id:guid}",
    (Guid id, SimulationStore store) =>
    {
        Simulation? simulation = store.Find(id);

        if (simulation is null)
        {
            return Results.NotFound();
        }

        SimulationResponse response = new()
        {
            Id = simulation.Id,
            Name = simulation.Name,
            BoidCount = simulation.BoidCount,
            IsRunning = simulation.IsRunning
        };

        return Results.Ok(response);
    });

app.Run();
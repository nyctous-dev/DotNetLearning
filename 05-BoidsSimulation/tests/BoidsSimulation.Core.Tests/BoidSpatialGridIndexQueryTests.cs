
using System.Numerics;
using BoidsSimulation.Core.Models;
using BoidsSimulation.Core.Simulation;

namespace BoidsSimulation.Core.Tests;

public sealed class BoidSpatialGridIndexQueryTests
{
    [Fact]
    public void QueryNeighborhoodIndices_ReturnsPopulationIndicesNotBoidIds()
    {
        // Arrange: deliberately use IDs that are not population indices.
        var population = new[]
        {
            CreateBoid(101, new Vector2(-1f, 0f)),
            CreateBoid(202, new Vector2(1f, 0f)),
            CreateBoid(303, new Vector2(30f, 30f))
        };

        var grid = new BoidSpatialGrid(
            worldWidth: 100,
            worldHeight: 100,
            cellSize: 10f,
            population);

        var destination = new int[10];

        // Act
        var count = grid.QueryNeighborhoodIndices(
            new Vector2(-1f, 0f),
            radius: 5f,
            destination);

        var results = destination.AsSpan(0, count).ToArray();

        // Assert
        Assert.Contains(0, results);
        Assert.Contains(1, results);
        Assert.DoesNotContain(2, results);

        Assert.All(
            results,
            index => Assert.InRange(index, 0, population.Length - 1));

        Assert.DoesNotContain(101, results);
        Assert.DoesNotContain(202, results);
        Assert.DoesNotContain(303, results);
    }

    [Fact]
    public void QueryNeighborhoodIndices_FindsBoidsAcrossWrappedWorldBoundary()
    {
        var population = new[]
        {
            CreateBoid(1001, new Vector2(-49f, 0f)),
            CreateBoid(2002, new Vector2(49f, 0f)),
            CreateBoid(3003, new Vector2(20f, 20f))
        };

        var grid = new BoidSpatialGrid(
            worldWidth: 100,
            worldHeight: 100,
            cellSize: 10f,
            population);

        var destination = new int[10];

        var count = grid.QueryNeighborhoodIndices(
            new Vector2(-49f, 0f),
            radius: 5f,
            destination);

        var results = destination.AsSpan(0, count).ToArray();

        Assert.Contains(0, results);
        Assert.Contains(1, results);
        Assert.DoesNotContain(2, results);
    }

    [Fact]
    public void QueryNeighborhoodIndices_DoesNotReturnDuplicateIndicesWhenQueryWraps()
    {
        var population = new[]
        {
            CreateBoid(700, new Vector2(-8f, -8f)),
            CreateBoid(800, new Vector2(0f, 0f)),
            CreateBoid(900, new Vector2(8f, 8f))
        };

        var grid = new BoidSpatialGrid(
            worldWidth: 20,
            worldHeight: 20,
            cellSize: 10f,
            population);

        var destination = new int[population.Length];

        var count = grid.QueryNeighborhoodIndices(
            Vector2.Zero,
            radius: 100f,
            destination);

        var results = destination.AsSpan(0, count).ToArray();

        Assert.Equal(population.Length, count);
        Assert.Equal(count, results.Distinct().Count());

        Assert.Equal(
            new[] { 0, 1, 2 },
            results.OrderBy(index => index));
    }

    [Fact]
    public void QueryNeighborhoodIndices_ReturnsZeroWhenDestinationIsEmpty()
    {
        var population = new[]
        {
            CreateBoid(101, Vector2.Zero)
        };

        var grid = new BoidSpatialGrid(
            worldWidth: 100,
            worldHeight: 100,
            cellSize: 10f,
            population);

        var count = grid.QueryNeighborhoodIndices(
            Vector2.Zero,
            radius: 10f,
            Span<int>.Empty);

        Assert.Equal(0, count);
    }

    [Fact]
    public void QueryNeighborhoodIndices_UsesCurrentPopulationIndicesAfterRebuild()
    {
        var initialPopulation = new[]
        {
            CreateBoid(100, new Vector2(30f, 30f)),
            CreateBoid(200, new Vector2(40f, 40f))
        };

        var grid = new BoidSpatialGrid(
            worldWidth: 100,
            worldHeight: 100,
            cellSize: 10f,
            initialPopulation);

        var updatedPopulation = new[]
        {
            CreateBoid(9001, new Vector2(-1f, 0f)),
            CreateBoid(9002, new Vector2(1f, 0f))
        };

        grid.Rebuild(updatedPopulation);

        var destination = new int[updatedPopulation.Length];

        var count = grid.QueryNeighborhoodIndices(
            new Vector2(-1f, 0f),
            radius: 5f,
            destination);

        var results = destination.AsSpan(0, count).ToArray();

        Assert.Contains(0, results);
        Assert.Contains(1, results);

        Assert.All(
            results,
            index => Assert.InRange(
                index,
                0,
                updatedPopulation.Length - 1));
    }

    [Fact]
    public void QueryNeighborhoodIndices_WithMetrics_ReportsVisitedCellsAndCandidates()
    {
        // Two boids occupy separate cells within the 3x3 search area.
        var population = new[]
        {
            CreateBoid(101, new Vector2(-1f, 0f)),
            CreateBoid(202, new Vector2(1f, 0f)),
            CreateBoid(303, new Vector2(30f, 30f))
        };

        var grid = new BoidSpatialGrid(
            worldWidth: 100,
            worldHeight: 100,
            cellSize: 10f,
            population);

        var destination = new int[10];

        // Act
        var count = grid.QueryNeighborhoodIndices(
            new Vector2(-1f, 0f),
            radius: 5f,
            destination,
            out var metrics);

        // Assert: a radius of 5 with cell size 10 searches 3x3 cells.
        Assert.Equal(2, count);
        Assert.Equal(9, metrics.CellsVisited);
        Assert.Equal(2, metrics.OccupiedCellsVisited);
        Assert.Equal(2, metrics.LinkedListEntriesTraversed);
        Assert.Equal(2, metrics.CandidatesReturned);
    }

    [Fact]
    public void QueryNeighborhoodIndices_WithMetricsAndEmptyDestination_ReportsNoWork()
    {
        var population = new[]
        {
            CreateBoid(101, Vector2.Zero)
        };

        var grid = new BoidSpatialGrid(
            worldWidth: 100,
            worldHeight: 100,
            cellSize: 10f,
            population);

        var count = grid.QueryNeighborhoodIndices(
            Vector2.Zero,
            radius: 10f,
            Span<int>.Empty,
            out var metrics);

        Assert.Equal(0, count);
        Assert.Equal(0, metrics.CellsVisited);
        Assert.Equal(0, metrics.OccupiedCellsVisited);
        Assert.Equal(0, metrics.LinkedListEntriesTraversed);
        Assert.Equal(0, metrics.CandidatesReturned);
    }

    private static Boid CreateBoid(int id, Vector2 position)
    {
        return new Boid(id, position, Vector2.Zero);
    }
}

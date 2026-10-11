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
        // Arrange: these boids are close together across the horizontal
        // edge of the wrapped world.
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

        // Act
        var count = grid.QueryNeighborhoodIndices(
            new Vector2(-49f, 0f),
            radius: 5f,
            destination);

        var results = destination.AsSpan(0, count).ToArray();

        // Assert
        Assert.Contains(0, results);
        Assert.Contains(1, results);
        Assert.DoesNotContain(2, results);
    }

    [Fact]
    public void QueryNeighborhoodIndices_DoesNotReturnDuplicateIndicesWhenQueryWraps()
    {
        // Arrange: the search radius covers the entire small world.
        // The query therefore wraps across the same cells multiple times
        // unless the grid avoids revisiting cells.
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

        // Act
        var count = grid.QueryNeighborhoodIndices(
            Vector2.Zero,
            radius: 100f,
            destination);

        var results = destination.AsSpan(0, count).ToArray();

        // Assert
        Assert.Equal(population.Length, count);
        Assert.Equal(count, results.Distinct().Count());

        Assert.Equal(
            new[] { 0, 1, 2 },
            results.OrderBy(index => index));
    }

    [Fact]
    public void QueryNeighborhoodIndices_ReturnsZeroWhenDestinationIsEmpty()
    {
        // Arrange
        var population = new[]
        {
            CreateBoid(101, Vector2.Zero)
        };

        var grid = new BoidSpatialGrid(
            worldWidth: 100,
            worldHeight: 100,
            cellSize: 10f,
            population);

        // Act
        var count = grid.QueryNeighborhoodIndices(
            Vector2.Zero,
            radius: 10f,
            Span<int>.Empty);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void QueryNeighborhoodIndices_UsesCurrentPopulationIndicesAfterRebuild()
    {
        // Arrange
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

        // Act
        var count = grid.QueryNeighborhoodIndices(
            new Vector2(-1f, 0f),
            radius: 5f,
            destination);

        var results = destination.AsSpan(0, count).ToArray();

        // Assert: indices refer to the new population's ordering,
        // not the IDs or ordering from the previous rebuild.
        Assert.Contains(0, results);
        Assert.Contains(1, results);

        Assert.All(
            results,
            index => Assert.InRange(
                index,
                0,
                updatedPopulation.Length - 1));
    }

    private static Boid CreateBoid(int id, Vector2 position)
    {
        return new Boid(id, position, Vector2.Zero);
    }
}
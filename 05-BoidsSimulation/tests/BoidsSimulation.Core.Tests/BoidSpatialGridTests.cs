using BoidsSimulation.Core.Models;
using BoidsSimulation.Core.Simulation;
using System.Numerics;

namespace BoidsSimulation.Core.Tests;

public class BoidSpatialGridTests
{
    private const int WorldWidth = 100;
    private const int WorldHeight = 100;
    private const float CellSize = 10f;

    [Fact]
    public void Constructor_AssignsBoidsToCorrectCells()
    {
        // Arrange
        var boids = new[]
        {
            CreateBoid(1, new Vector2(-40f, -40f)),
            CreateBoid(2, new Vector2(0f, 0f)),
            CreateBoid(3, new Vector2(40f, 40f))
        };

        // Act
        var grid = CreateGrid(boids);

        // Assert
        Assert.Equal(new[] { 1 }, grid.GetBoidIdsInCell(1, 1));
        Assert.Equal(new[] { 2 }, grid.GetBoidIdsInCell(5, 5));
        Assert.Equal(new[] { 3 }, grid.GetBoidIdsInCell(9, 9));
    }

    [Fact]
    public void QueryNeighborhood_ReturnsBoidsFromNearbyCells()
    {
        // Arrange
        var boids = new[]
        {
            CreateBoid(1, new Vector2(0f, 0f)),
            CreateBoid(2, new Vector2(12f, 0f)),
            CreateBoid(3, new Vector2(0f, 12f)),
            CreateBoid(4, new Vector2(30f, 30f))
        };

        var grid = CreateGrid(boids);

        // Act
        var result = grid.QueryNeighborhood(
            center: new Vector2(0f, 0f),
            radius: 10f,
            maxCandidates: 50);

        // Assert
        Assert.Contains(1, result);
        Assert.Contains(2, result);
        Assert.Contains(3, result);
        Assert.DoesNotContain(4, result);
    }

    [Fact]
    public void QueryNeighborhood_FindsBoidsAcrossWorldBoundary()
    {
        // Arrange
        var boids = new[]
        {
            CreateBoid(1, new Vector2(49f, 0f)),
            CreateBoid(2, new Vector2(-49f, 0f)),
            CreateBoid(3, new Vector2(0f, 0f))
        };

        var grid = CreateGrid(boids);

        // Act
        var result = grid.QueryNeighborhood(
            center: new Vector2(49f, 0f),
            radius: 5f,
            maxCandidates: 50);

        // Assert
        Assert.Contains(1, result);
        Assert.Contains(2, result);
        Assert.DoesNotContain(3, result);
    }

    [Fact]
    public void QueryNeighborhood_DoesNotReturnDuplicateBoids_WhenWorldWrappingRevisitsCells()
    {
        // Arrange
        const int smallWorldWidth = 10;
        const int smallWorldHeight = 10;
        const float smallCellSize = 5f;

        var boids = new[]
        {
            CreateBoid(1, new Vector2(4.9f, 4.9f)),
            CreateBoid(2, new Vector2(-4.9f, -4.9f))
        };

        var grid = new BoidSpatialGrid(
            smallWorldWidth,
            smallWorldHeight,
            smallCellSize,
            boids);

        // Act
        var result = grid.QueryNeighborhood(
            center: new Vector2(4.9f, 4.9f),
            radius: 5f,
            maxCandidates: 50);

        // Assert
        Assert.Equal(result.Count, result.Distinct().Count());
        Assert.Contains(1, result);
        Assert.Contains(2, result);
    }

    [Fact]
    public void QueryNeighborhood_SkipsEmptyCells()
    {
        // Arrange
        var boids = new[]
        {
            CreateBoid(1, new Vector2(0f, 0f)),
            CreateBoid(2, new Vector2(10f, 0f))
        };

        var grid = CreateGrid(boids);

        // Act
        var result = grid.QueryNeighborhood(
            center: new Vector2(0f, 0f),
            radius: 10f,
            maxCandidates: 50);

        // Assert
        Assert.Contains(1, result);
        Assert.Contains(2, result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void QueryNeighborhood_RespectsMaximumCandidateCount()
    {
        // Arrange
        var boids = Enumerable
            .Range(1, 10)
            .Select(id => CreateBoid(id, new Vector2(0f, 0f)))
            .ToArray();

        var grid = CreateGrid(boids);

        // Act
        var result = grid.QueryNeighborhood(
            center: Vector2.Zero,
            radius: 10f,
            maxCandidates: 3);

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Rebuild_ReplacesExistingPopulation()
    {
        // Arrange
        var initialPopulation = new[]
        {
            CreateBoid(1, new Vector2(0f, 0f))
        };

        var replacementPopulation = new[]
        {
            CreateBoid(2, new Vector2(20f, 20f))
        };

        var grid = CreateGrid(initialPopulation);

        // Act
        grid.Rebuild(replacementPopulation);

        // Assert
        var oldCell = grid.GetBoidIdsInCell(5, 5);
        var newCell = grid.GetBoidIdsInCell(7, 7);

        Assert.Empty(oldCell);
        Assert.Equal(new[] { 2 }, newCell);
    }

    [Fact]
    public void Constructor_AcceptsEmptyPopulation()
    {
        // Arrange
        var boids = Array.Empty<Boid>();

        // Act
        var grid = CreateGrid(boids);

        // Assert
        Assert.Empty(
            grid.QueryNeighborhood(
                center: Vector2.Zero,
                radius: 10f,
                maxCandidates: 50));
    }

    [Fact]
    public void QueryNeighborhood_ReturnsEmpty_WhenMaximumCandidateCountIsZero()
    {
        // Arrange
        var boids = new[]
        {
            CreateBoid(1, Vector2.Zero)
        };

        var grid = CreateGrid(boids);

        // Act
        var result = grid.QueryNeighborhood(
            center: Vector2.Zero,
            radius: 10f,
            maxCandidates: 0);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void QueryNeighborhood_Throws_WhenMaximumCandidateCountIsNegative()
    {
        // Arrange
        var grid = CreateGrid(
            new[] { CreateBoid(1, Vector2.Zero) });

        // Act
        var action = () => grid.QueryNeighborhood(
            center: Vector2.Zero,
            radius: 10f,
            maxCandidates: -1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void Constructor_Throws_WhenWorldWidthIsNotPositive()
    {
        // Arrange
        var boids = Array.Empty<Boid>();

        // Act
        var action = () => new BoidSpatialGrid(
            0,
            WorldHeight,
            CellSize,
            boids);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void Constructor_Throws_WhenWorldHeightIsNotPositive()
    {
        // Arrange
        var boids = Array.Empty<Boid>();

        // Act
        var action = () => new BoidSpatialGrid(
            WorldWidth,
            0,
            CellSize,
            boids);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void Constructor_Throws_WhenCellSizeIsNotPositive()
    {
        // Arrange
        var boids = Array.Empty<Boid>();

        // Act
        var action = () => new BoidSpatialGrid(
            WorldWidth,
            WorldHeight,
            0f,
            boids);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void Constructor_Throws_WhenCellSizeIsNotFinite()
    {
        // Arrange
        var boids = Array.Empty<Boid>();

        // Act
        var action = () => new BoidSpatialGrid(
            WorldWidth,
            WorldHeight,
            float.NaN,
            boids);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void Rebuild_Throws_WhenPopulationContainsDuplicateIds()
    {
        // Arrange
        var boids = new[]
        {
            CreateBoid(1, Vector2.Zero),
            CreateBoid(1, new Vector2(10f, 10f))
        };

        var grid = CreateGrid(Array.Empty<Boid>());

        // Act
        var action = () => grid.Rebuild(boids);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    private static BoidSpatialGrid CreateGrid(IReadOnlyList<Boid> boids)
    {
        return new BoidSpatialGrid(
            WorldWidth,
            WorldHeight,
            CellSize,
            boids);
    }

    private static Boid CreateBoid(int id, Vector2 position)
    {
        return new Boid(
            id,
            position,
            Vector2.Zero);
    }
}
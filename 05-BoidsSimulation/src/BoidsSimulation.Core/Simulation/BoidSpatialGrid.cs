
using BoidsSimulation.Core.Models;
using System.Numerics;

namespace BoidsSimulation.Core.Simulation;

public sealed class BoidSpatialGrid
{
    private readonly int _worldWidth;
    private readonly int _worldHeight;
    private readonly float _cellSize;
    private readonly int _columns;
    private readonly int _rows;

    private readonly Dictionary<int, List<Boid>> _cells = new();

    public BoidSpatialGrid(
        int worldWidth,
        int worldHeight,
        float cellSize,
        IReadOnlyList<Boid> population)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(worldWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(worldHeight);

        if (!float.IsFinite(cellSize) || cellSize <= 0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cellSize),
                "Cell size must be finite and greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(population);

        _worldWidth = worldWidth;
        _worldHeight = worldHeight;
        _cellSize = cellSize;

        _columns = CalculateCellCount(worldWidth, cellSize);
        _rows = CalculateCellCount(worldHeight, cellSize);

        Rebuild(population);
    }

    public void Rebuild(IReadOnlyList<Boid> population)
    {
        ArgumentNullException.ThrowIfNull(population);

        _cells.Clear();

        var seenIds = new HashSet<int>();

        foreach (var boid in population)
        {
            ArgumentNullException.ThrowIfNull(boid);

            if (!seenIds.Add(boid.Id))
            {
                throw new ArgumentException(
                    $"Population contains duplicate boid ID {boid.Id}.",
                    nameof(population));
            }

            var (column, row) = GetCellCoordinates(boid.Position);
            var cellKey = GetCellKey(column, row);

            if (!_cells.TryGetValue(cellKey, out var cell))
            {
                cell = new List<Boid>();
                _cells.Add(cellKey, cell);
            }

            cell.Add(boid);
        }
    }

    public IReadOnlyList<int> GetBoidIdsInCell(int column, int row)
    {
        if (column < 0 || column >= _columns)
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }

        if (row < 0 || row >= _rows)
        {
            throw new ArgumentOutOfRangeException(nameof(row));
        }

        var cellKey = GetCellKey(column, row);

        if (!_cells.TryGetValue(cellKey, out var cell))
        {
            return Array.Empty<int>();
        }

        return cell.Select(boid => boid.Id).ToArray();
    }

    public IReadOnlyList<int> QueryNeighborhood(
        Vector2 center,
        float radius,
        int maxCandidates)
    {
        if (!float.IsFinite(center.X) || !float.IsFinite(center.Y))
        {
            throw new ArgumentOutOfRangeException(
                nameof(center),
                "Query center coordinates must be finite.");
        }

        if (!float.IsFinite(radius) || radius < 0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(radius),
                "Radius must be finite and non-negative.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(maxCandidates);

        if (maxCandidates == 0 || _cells.Count == 0)
        {
            return Array.Empty<int>();
        }

        var (centerColumn, centerRow) = GetCellCoordinates(center);

        var cellRange = (int)Math.Ceiling(radius / _cellSize);

        // A query covering at least half the cell count in either
        // direction may revisit wrapped cells. Track visited cells
        // so neither cells nor boids are returned more than once.
        var visitedCells = new HashSet<int>();
        var result = new List<int>(Math.Min(maxCandidates, _cells.Count));

        for (var rowOffset = -cellRange; rowOffset <= cellRange; rowOffset++)
        {
            var row = WrapCellIndex(centerRow + rowOffset, _rows);

            for (var columnOffset = -cellRange;
                 columnOffset <= cellRange;
                 columnOffset++)
            {
                var column = WrapCellIndex(centerColumn + columnOffset, _columns);
                var cellKey = GetCellKey(column, row);

                if (!visitedCells.Add(cellKey))
                {
                    continue;
                }

                if (!_cells.TryGetValue(cellKey, out var cell))
                {
                    continue;
                }

                foreach (var boid in cell)
                {
                    result.Add(boid.Id);

                    if (result.Count >= maxCandidates)
                    {
                        return result;
                    }
                }
            }
        }

        return result;
    }

    private (int Column, int Row) GetCellCoordinates(Vector2 position)
    {
        var wrappedX = WrapCoordinate(position.X, _worldWidth);
        var wrappedY = WrapCoordinate(position.Y, _worldHeight);

        var column = (int)Math.Floor(
            (wrappedX + _worldWidth / 2f) / _cellSize);

        var row = (int)Math.Floor(
            (wrappedY + _worldHeight / 2f) / _cellSize);

        // Protect against floating-point rounding at the upper boundary.
        column = Math.Clamp(column, 0, _columns - 1);
        row = Math.Clamp(row, 0, _rows - 1);

        return (column, row);
    }

    private static int CalculateCellCount(int worldDimension, float cellSize)
    {
        return Math.Max(1, (int)Math.Ceiling(worldDimension / (double)cellSize));
    }

    private int GetCellKey(int column, int row)
    {
        return row * _columns + column;
    }

    private static int WrapCellIndex(int index, int cellCount)
    {
        return ((index % cellCount) + cellCount) % cellCount;
    }

    private static float WrapCoordinate(float coordinate, int worldDimension)
    {
        var halfDimension = worldDimension / 2f;
        var wrapped = (coordinate + halfDimension) % worldDimension;

        if (wrapped < 0f)
        {
            wrapped += worldDimension;
        }

        return wrapped - halfDimension;
    }
}
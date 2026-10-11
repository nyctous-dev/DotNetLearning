
using System.Numerics;
using BoidsSimulation.Core.Models;

namespace BoidsSimulation.Core.Simulation;

public sealed class BoidSpatialGrid
{
    private readonly int _worldWidth;
    private readonly int _worldHeight;
    private readonly float _cellSize;
    private readonly int _columns;
    private readonly int _rows;

    // Each occupied cell points to the first boid index in a linked chain.
    // The dictionary is sparse, so a small cell size does not require a
    // massive array for every possible cell in the world.
    private readonly Dictionary<long, int> _cellHeads = new();

    // Reused to validate unique IDs when the population is rebuilt.
    private readonly HashSet<int> _seenIds = new();

    // These arrays grow when necessary and are reused by later rebuilds.
    private Boid[] _boids = Array.Empty<Boid>();
    private int[] _nextBoidIndices = Array.Empty<int>();
    private int _boidCount;

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

        // Validate the incoming population before changing the current grid.
        _seenIds.Clear();

        for (var i = 0; i < population.Count; i++)
        {
            var boid = population[i];

            ArgumentNullException.ThrowIfNull(boid);

            if (!_seenIds.Add(boid.Id))
            {
                throw new ArgumentException(
                    $"Population contains duplicate boid ID {boid.Id}.",
                    nameof(population));
            }

            if (!float.IsFinite(boid.Position.X) ||
                !float.IsFinite(boid.Position.Y))
            {
                throw new ArgumentException(
                    $"Boid {boid.Id} has a non-finite position.",
                    nameof(population));
            }
        }

        EnsureCapacity(population.Count);

        _cellHeads.Clear();
        _boidCount = population.Count;

        for (var i = 0; i < population.Count; i++)
        {
            var boid = population[i];
            var (column, row) = GetCellCoordinates(boid.Position);
            var cellKey = GetCellKey(column, row);

            _boids[i] = boid;

            // Insert this boid at the head of its cell's linked chain.
            if (_cellHeads.TryGetValue(cellKey, out var currentHead))
            {
                _nextBoidIndices[i] = currentHead;
                _cellHeads[cellKey] = i;
            }
            else
            {
                _nextBoidIndices[i] = -1;
                _cellHeads.Add(cellKey, i);
            }
        }
    }

    /// <summary>
    /// Returns the IDs of boids currently assigned to the specified cell.
    /// Intended mainly for diagnostics and tests.
    /// </summary>
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

        if (!_cellHeads.TryGetValue(cellKey, out var currentIndex))
        {
            return Array.Empty<int>();
        }

        var count = 0;

        for (var index = currentIndex;
             index != -1;
             index = _nextBoidIndices[index])
        {
            count++;
        }

        var ids = new int[count];
        var destinationIndex = 0;

        for (var index = currentIndex;
             index != -1;
             index = _nextBoidIndices[index])
        {
            ids[destinationIndex++] = _boids[index].Id;
        }

        return ids;
    }

    /// <summary>
    /// Writes neighborhood candidate IDs into a caller-provided buffer.
    /// Returns the number of IDs written, which never exceeds the buffer
    /// length. The caller can reuse the same buffer for subsequent queries.
    ///
    /// Results are cell-based candidates, not an exact distance-filtered
    /// neighborhood. The caller must check actual distances.
    /// </summary>
    public int QueryNeighborhood(
        Vector2 center,
        float radius,
        Span<int> destination)
    {
        ValidateQuery(center, radius);

        if (destination.IsEmpty || _boidCount == 0)
        {
            return 0;
        }

        var (centerColumn, centerRow) = GetCellCoordinates(center);

        var radiusInCells = Math.Ceiling((double)radius / _cellSize);

        var visitAllColumns =
            radiusInCells >= (_columns - 1) / 2d;

        var visitAllRows =
            radiusInCells >= (_rows - 1) / 2d;

        // If the query covers the whole axis, visit each cell once.
        // Otherwise, visit the wrapped sequence around the center cell.
        // This avoids a per-query HashSet just to deduplicate wrapped cells.
        var columnsToVisit = visitAllColumns
            ? _columns
            : (int)(2d * radiusInCells + 1d);

        var rowsToVisit = visitAllRows
            ? _rows
            : (int)(2d * radiusInCells + 1d);

        var columnRange = visitAllColumns
            ? 0L
            : (long)radiusInCells;

        var rowRange = visitAllRows
            ? 0L
            : (long)radiusInCells;

        var count = 0;

        for (var rowStep = 0; rowStep < rowsToVisit; rowStep++)
        {
            var row = visitAllRows
                ? rowStep
                : WrapCellIndex(
                    (long)centerRow - rowRange + rowStep,
                    _rows);

            for (var columnStep = 0;
                 columnStep < columnsToVisit;
                 columnStep++)
            {
                var column = visitAllColumns
                    ? columnStep
                    : WrapCellIndex(
                        (long)centerColumn - columnRange + columnStep,
                        _columns);

                var cellKey = GetCellKey(column, row);

                if (!_cellHeads.TryGetValue(cellKey, out var index))
                {
                    continue;
                }

                while (index != -1)
                {
                    if (count >= destination.Length)
                    {
                        return count;
                    }

                    destination[count++] = _boids[index].Id;
                    index = _nextBoidIndices[index];
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Compatibility overload for callers that want an allocated result.
    /// Performance-critical code should use the destination-buffer overload.
    /// </summary>
    public IReadOnlyList<int> QueryNeighborhood(
        Vector2 center,
        float radius,
        int maxCandidates)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxCandidates);
        ValidateQuery(center, radius);

        if (maxCandidates == 0 || _boidCount == 0)
        {
            return Array.Empty<int>();
        }

        var buffer = new int[Math.Min(maxCandidates, _boidCount)];

        var count = QueryNeighborhood(
            center,
            radius,
            buffer.AsSpan());

        if (count == 0)
        {
            return Array.Empty<int>();
        }

        if (count < buffer.Length)
        {
            Array.Resize(ref buffer, count);
        }

        return buffer;
    }

    private void ValidateQuery(Vector2 center, float radius)
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
    }

    private void EnsureCapacity(int requiredCapacity)
    {
        if (_boids.Length >= requiredCapacity)
        {
            return;
        }

        var doubledCapacity = _boids.Length == 0
            ? 16
            : (long)_boids.Length * 2;

        var newCapacity = (int)Math.Min(
            int.MaxValue,
            Math.Max(requiredCapacity, doubledCapacity));

        Array.Resize(ref _boids, newCapacity);
        Array.Resize(ref _nextBoidIndices, newCapacity);
    }

    private (int Column, int Row) GetCellCoordinates(Vector2 position)
    {
        var wrappedX = WrapCoordinate(position.X, _worldWidth);
        var wrappedY = WrapCoordinate(position.Y, _worldHeight);

        var column = (int)Math.Floor(
            (wrappedX + _worldWidth / 2d) / _cellSize);

        var row = (int)Math.Floor(
            (wrappedY + _worldHeight / 2d) / _cellSize);

        // Protect against floating-point rounding near the positive edge.
        column = Math.Clamp(column, 0, _columns - 1);
        row = Math.Clamp(row, 0, _rows - 1);

        return (column, row);
    }

    private static int CalculateCellCount(
        int worldDimension,
        float cellSize)
    {
        var count = Math.Ceiling(worldDimension / (double)cellSize);

        if (count > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cellSize),
                "Cell size would produce too many grid columns or rows.");
        }

        return Math.Max(1, (int)count);
    }

    private long GetCellKey(int column, int row)
    {
        return (long)row * _columns + column;
    }

    private static int WrapCellIndex(long index, int cellCount)
    {
        return (int)(((index % cellCount) + cellCount) % cellCount);
    }

    private static float WrapCoordinate(
        float coordinate,
        int worldDimension)
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

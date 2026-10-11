using System.Numerics;
using BoidsSimulation.Core.Models;

namespace BoidsSimulation.Core.Simulation;

/// <summary>
/// Work performed by one spatial-grid neighborhood query.
/// </summary>
public readonly record struct BoidSpatialGridQueryMetrics(
    int CellsVisited,
    int OccupiedCellsVisited,
    int LinkedListEntriesTraversed,
    int CandidatesReturned);

public sealed class BoidSpatialGrid
{
    private readonly int _worldWidth;
    private readonly int _worldHeight;
    private readonly float _cellSize;
    private readonly float _halfWorldWidth;
    private readonly float _halfWorldHeight;
    private readonly int _columnCount;
    private readonly int _rowCount;

    // Maps each occupied cell to the first population index in that cell.
    private readonly Dictionary<long, int> _cellHeads = new();

    // Reused when validating IDs during a rebuild.
    private readonly HashSet<int> _seenIds = new();

    // The array position is the boid's index in the current population.
    private Boid[] _boids = Array.Empty<Boid>();

    // Singly linked lists connect population indices within each cell.
    private int[] _nextBoidIndices = Array.Empty<int>();

    private int _boidCount;

    public BoidSpatialGrid(
        int worldWidth,
        int worldHeight,
        float cellSize,
        IReadOnlyList<Boid> population)
    {
        if (worldWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(worldWidth),
                "World width must be positive.");
        }

        if (worldHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(worldHeight),
                "World height must be positive.");
        }

        if (!float.IsFinite(cellSize) || cellSize <= 0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cellSize),
                "Cell size must be finite and positive.");
        }

        _worldWidth = worldWidth;
        _worldHeight = worldHeight;
        _cellSize = cellSize;

        _halfWorldWidth = worldWidth / 2f;
        _halfWorldHeight = worldHeight / 2f;

        _columnCount = CalculateCellCount(worldWidth, cellSize);
        _rowCount = CalculateCellCount(worldHeight, cellSize);

        Rebuild(population);
    }

    public void Rebuild(IReadOnlyList<Boid> population)
    {
        ArgumentNullException.ThrowIfNull(population);

        var populationCount = population.Count;

        // Validate the complete input before replacing the current grid.
        _seenIds.Clear();

        for (var i = 0; i < populationCount; i++)
        {
            var boid = population[i];

            if (boid is null)
            {
                throw new ArgumentException(
                    "Population cannot contain null boids.",
                    nameof(population));
            }

            if (!float.IsFinite(boid.Position.X) ||
                !float.IsFinite(boid.Position.Y))
            {
                throw new ArgumentException(
                    $"Boid {boid.Id} has a non-finite position.",
                    nameof(population));
            }

            if (!_seenIds.Add(boid.Id))
            {
                throw new ArgumentException(
                    $"Population contains duplicate boid ID {boid.Id}.",
                    nameof(population));
            }
        }

        EnsureCapacity(populationCount);

        _cellHeads.Clear();
        _boidCount = populationCount;

        for (var populationIndex = 0;
             populationIndex < populationCount;
             populationIndex++)
        {
            var boid = population[populationIndex];

            _boids[populationIndex] = boid;

            GetCellCoordinates(
                boid.Position,
                out var column,
                out var row);

            var cellKey = GetCellKey(column, row);

            // Insert this population index at the head of its cell's list.
            if (_cellHeads.TryGetValue(cellKey, out var previousHead))
            {
                _nextBoidIndices[populationIndex] = previousHead;
            }
            else
            {
                _nextBoidIndices[populationIndex] = -1;
            }

            _cellHeads[cellKey] = populationIndex;
        }
    }

    /// <summary>
    /// Returns boid IDs from cells intersecting the query's search area.
    /// Results are candidates; callers must check exact distances.
    /// </summary>
    public int QueryNeighborhood(
        Vector2 center,
        float radius,
        Span<int> destination)
    {
        return QueryNeighborhoodCore(
            center,
            radius,
            destination,
            returnPopulationIndices: false,
            out _);
    }

    /// <summary>
    /// Returns population indices from cells intersecting the query's
    /// search area. An index refers to the population supplied to the
    /// most recent Rebuild call.
    ///
    /// Results are candidates; callers must check exact distances.
    /// </summary>
    public int QueryNeighborhoodIndices(
        Vector2 center,
        float radius,
        Span<int> destination)
    {
        return QueryNeighborhoodCore(
            center,
            radius,
            destination,
            returnPopulationIndices: true,
            out _);
    }

    /// <summary>
    /// Returns population indices and reports the work performed by the query.
    /// The metrics are value data and do not allocate per query.
    /// </summary>
    public int QueryNeighborhoodIndices(
        Vector2 center,
        float radius,
        Span<int> destination,
        out BoidSpatialGridQueryMetrics metrics)
    {
        return QueryNeighborhoodCore(
            center,
            radius,
            destination,
            returnPopulationIndices: true,
            out metrics);
    }

    /// <summary>
    /// Compatibility overload that returns a collection of boid IDs.
    /// This overload allocates its result buffer.
    /// </summary>
    public IReadOnlyList<int> QueryNeighborhood(
        Vector2 center,
        float radius,
        int maxCandidates)
    {
        if (maxCandidates < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCandidates),
                "Maximum candidates cannot be negative.");
        }

        if (maxCandidates == 0 || _boidCount == 0)
        {
            return Array.Empty<int>();
        }

        var buffer = new int[Math.Min(maxCandidates, _boidCount)];

        var count = QueryNeighborhood(
            center,
            radius,
            buffer.AsSpan());

        if (count == buffer.Length)
        {
            return buffer;
        }

        Array.Resize(ref buffer, count);

        return buffer;
    }

    /// <summary>
    /// Returns the boid IDs currently stored in the specified cell.
    /// Intended for diagnostics and tests, not the simulation hot path.
    /// </summary>
    public IReadOnlyList<int> GetBoidIdsInCell(
        int column,
        int row)
    {
        ValidateCellCoordinates(column, row);

        var cellKey = GetCellKey(column, row);

        if (!_cellHeads.TryGetValue(cellKey, out var populationIndex))
        {
            return Array.Empty<int>();
        }

        var result = new List<int>();

        while (populationIndex != -1)
        {
            result.Add(_boids[populationIndex].Id);
            populationIndex = _nextBoidIndices[populationIndex];
        }

        return result;
    }

    private int QueryNeighborhoodCore(
        Vector2 center,
        float radius,
        Span<int> destination,
        bool returnPopulationIndices,
        out BoidSpatialGridQueryMetrics metrics)
    {
        ValidateQuery(center, radius);

        var cellsVisited = 0;
        var occupiedCellsVisited = 0;
        var linkedListEntriesTraversed = 0;
        var resultCount = 0;

        if (destination.IsEmpty || _boidCount == 0)
        {
            metrics = new BoidSpatialGridQueryMetrics(0, 0, 0, 0);
            return 0;
        }

        GetCellCoordinates(
            center,
            out var centerColumn,
            out var centerRow);

        var radiusInCells = CalculateSearchCellRadius(radius);

        var visitAllColumns =
            (2 * radiusInCells) + 1 >= _columnCount;

        var visitAllRows =
            (2 * radiusInCells) + 1 >= _rowCount;

        var columnVisits = visitAllColumns
            ? _columnCount
            : (2 * radiusInCells) + 1;

        var rowVisits = visitAllRows
            ? _rowCount
            : (2 * radiusInCells) + 1;

        // When every cell on an axis must be visited, walk each cell
        // exactly once rather than revisiting cells through world wrapping.
        for (var rowVisit = 0; rowVisit < rowVisits; rowVisit++)
        {
            var rowOffset = visitAllRows
                ? rowVisit
                : rowVisit - radiusInCells;

            var row = WrapCellIndex(
                centerRow + rowOffset,
                _rowCount);

            for (var columnVisit = 0;
                 columnVisit < columnVisits;
                 columnVisit++)
            {
                var columnOffset = visitAllColumns
                    ? columnVisit
                    : columnVisit - radiusInCells;

                var column = WrapCellIndex(
                    centerColumn + columnOffset,
                    _columnCount);

                cellsVisited++;

                var cellKey = GetCellKey(column, row);

                if (!_cellHeads.TryGetValue(
                        cellKey,
                        out var populationIndex))
                {
                    continue;
                }

                occupiedCellsVisited++;

                while (populationIndex != -1)
                {
                    linkedListEntriesTraversed++;

                    if (resultCount >= destination.Length)
                    {
                        metrics = new BoidSpatialGridQueryMetrics(
                            cellsVisited,
                            occupiedCellsVisited,
                            linkedListEntriesTraversed,
                            resultCount);
                        return resultCount;
                    }

                    destination[resultCount++] = returnPopulationIndices
                        ? populationIndex
                        : _boids[populationIndex].Id;

                    populationIndex =
                        _nextBoidIndices[populationIndex];
                }
            }
        }

        metrics = new BoidSpatialGridQueryMetrics(
            cellsVisited,
            occupiedCellsVisited,
            linkedListEntriesTraversed,
            resultCount);

        return resultCount;
    }

    private void ValidateQuery(Vector2 center, float radius)
    {
        if (!float.IsFinite(center.X) ||
            !float.IsFinite(center.Y))
        {
            throw new ArgumentOutOfRangeException(
                nameof(center),
                "Query center coordinates must be finite.");
        }

        if (!float.IsFinite(radius) || radius < 0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(radius),
                "Query radius must be finite and non-negative.");
        }
    }

    private void ValidateCellCoordinates(int column, int row)
    {
        if (column < 0 || column >= _columnCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(column),
                $"Column must be between 0 and {_columnCount - 1}.");
        }

        if (row < 0 || row >= _rowCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(row),
                $"Row must be between 0 and {_rowCount - 1}.");
        }
    }

    private int CalculateSearchCellRadius(float radius)
    {
        // Cap the result because searching farther than the grid's
        // largest dimension cannot uncover any additional cells.
        var maximumUsefulRadius = Math.Max(
            _columnCount,
            _rowCount);

        var requiredRadius = Math.Ceiling(
            (double)radius / _cellSize);

        return (int)Math.Min(
            requiredRadius,
            maximumUsefulRadius);
    }

    private void GetCellCoordinates(
        Vector2 position,
        out int column,
        out int row)
    {
        var wrappedX = WrapCoordinate(
            position.X,
            _worldWidth,
            _halfWorldWidth);

        var wrappedY = WrapCoordinate(
            position.Y,
            _worldHeight,
            _halfWorldHeight);

        column = (int)MathF.Floor(
            (wrappedX + _halfWorldWidth) / _cellSize);

        row = (int)MathF.Floor(
            (wrappedY + _halfWorldHeight) / _cellSize);

        // The final cell can be narrower when the world dimensions
        // are not exact multiples of the cell size.
        column = Math.Clamp(column, 0, _columnCount - 1);
        row = Math.Clamp(row, 0, _rowCount - 1);
    }

    private static int CalculateCellCount(
        int worldDimension,
        float cellSize)
    {
        return Math.Max(
            1,
            (int)Math.Ceiling(worldDimension / (double)cellSize));
    }

    private static float WrapCoordinate(
        float coordinate,
        float worldDimension,
        float halfWorldDimension)
    {
        var wrapped = coordinate % worldDimension;

        if (wrapped < -halfWorldDimension)
        {
            wrapped += worldDimension;
        }
        else if (wrapped >= halfWorldDimension)
        {
            wrapped -= worldDimension;
        }

        return wrapped;
    }

    private static int WrapCellIndex(int index, int cellCount)
    {
        var wrapped = index % cellCount;

        return wrapped < 0
            ? wrapped + cellCount
            : wrapped;
    }

    private long GetCellKey(int column, int row)
    {
        return ((long)row * _columnCount) + column;
    }

    private void EnsureCapacity(int requiredCapacity)
    {
        if (_boids.Length < requiredCapacity)
        {
            var newCapacity = Math.Max(
                requiredCapacity,
                Math.Max(4, _boids.Length * 2));

            Array.Resize(ref _boids, newCapacity);
        }

        if (_nextBoidIndices.Length < requiredCapacity)
        {
            var newCapacity = Math.Max(
                requiredCapacity,
                Math.Max(4, _nextBoidIndices.Length * 2));

            Array.Resize(ref _nextBoidIndices, newCapacity);
        }
    }
}
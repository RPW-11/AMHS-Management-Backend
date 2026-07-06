using Domain.Common.Models;
using Domain.Errors.Missions.RoutePlanning;
using FluentResults;

namespace Domain.Missions.ValueObjects;

public sealed class Grid : ValueObject
{
    private const int MinRowDim = 3;
    private const int MinColDim = 3;
    private const int MinWidthLength = 1;
    private const int MinHeightLength = 1;

    public static class MapTrajectory
    {
        public static readonly int[] Up = [1, 0];
        public static readonly int[] Down = [-1, 0];
        public static readonly int[] Left = [0, -1];
        public static readonly int[] Right = [0, 1];
        public static readonly int[][] AllDirections =
        [
            Up, Down, Left, Right,
        ];
    }

    public int RowDim { get; }
    public int ColDim { get; }
    public int WidthLength { get; }
    public int HeightLength { get; }
    public readonly PathPoint[,] MapMatrix;

    private Grid(int rowDim, int colDim, int widthLength, int heightLength, PathPoint[,] mapMatrix)
    {
        RowDim = rowDim;
        ColDim = colDim;
        WidthLength = widthLength;
        HeightLength = heightLength;
        MapMatrix = mapMatrix;
    }

    public static Result<Grid> Create(
        int rowDim,
        int colDim,
        int widthLength,
        int heightLength,
        List<PathPoint> points
        )
    {
        if (rowDim < MinRowDim && colDim < MinColDim)
        {
            return Result.Fail($"Row and Column dimension must be at least {MinColDim}");
        }

        if (widthLength < MinWidthLength && heightLength < MinHeightLength)
        {
            return Result.Fail($"Width and Height dimension must be at least {MinHeightLength}");
        }

        var mapMatrixResult = CreateMapMatrix(rowDim, colDim, points);
        if (mapMatrixResult.IsFailed)
        {
            return Result.Fail(mapMatrixResult.Errors);
        }

        return new Grid(rowDim, colDim, widthLength, heightLength, mapMatrixResult.Value);
    }

    private static Result<PathPoint[,]> CreateMapMatrix(int rowDim, int colDim, List<PathPoint> points)
    {
        var mapMatrix = new PathPoint[rowDim, colDim];

        for (int rowPos = 0; rowPos < rowDim; rowPos++)
        {
            for (int colPos = 0; colPos < colDim; colPos++)
            {
                mapMatrix[rowPos, colPos] = PointFactory.Create(PointCategory.Path, rowPos, colPos, null, null);
            }
        }

        foreach (PathPoint p in points)
        {
            if (p.RowPos < 0 || p.RowPos >= rowDim)
            {
                return Result.Fail<PathPoint[,]>(new InvalidRowPosValueError(p.RowPos, rowDim));
            }
            if (p.ColPos < 0 || p.ColPos >= colDim)
            {
                return Result.Fail<PathPoint[,]>(new InvalidColPosValueError(p.ColPos, colDim));
            }

            mapMatrix[p.RowPos, p.ColPos] = p;
        }

        return Result.Ok(mapMatrix);
    }

    public PathPoint? GetPointAt(int rowPos, int colPos)
    {
        if (rowPos < 0 || rowPos > RowDim - 1 || colPos < 0 || colPos > ColDim - 1)
        {
            return null;
        }

        return MapMatrix[rowPos, colPos];
    }

    public int GetSquareLength()
    {
        var perSquareArea = WidthLength * HeightLength / (RowDim * ColDim);
        return (int)Math.Sqrt(perSquareArea);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return RowDim;
        yield return ColDim;
        yield return MapMatrix;
    }
}

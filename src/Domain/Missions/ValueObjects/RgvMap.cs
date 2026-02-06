using Domain.Common.Models;
using Domain.Errors.Missions.RoutePlanning;
using FluentResults;

namespace Domain.Missions.ValueObjects;

public sealed class RgvMap : ValueObject
{
    private const int MinRowDim = 3;
    private const int MinColDim = 3;
    private const int MinWidthLength = 1;
    private const int MinHeightLength = 1;
    private const int MinNumberStationsOrder = 2;

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
    public int RowDim { get; private set; }
    public int ColDim { get; private set; }
    public int WidthLength { get; private set; }
    public int HeightLength { get; private set; }
    public List<PathPoint> StationsOrder { get; }
    public List<PathPoint> Solution { get; private set; }
    public string PathColor { get; private set; }
    public readonly PathPoint[,] MapMatrix;

    public RgvMap(int rowDim, int colDim, int widthLength, int heightLength, List<PathPoint> stationsOrder, PathPoint[,] mapMatrix, List<PathPoint> solution, string pathColor)
    {
        RowDim = rowDim;
        ColDim = colDim;
        WidthLength = widthLength;
        HeightLength = heightLength;
        StationsOrder = stationsOrder;
        Solution = solution;
        PathColor = pathColor;
        MapMatrix = mapMatrix;
    }

    public static Result<RgvMap> Create(
        int rowDim,
        int colDim, 
        int widthLength, 
        int heightLength, 
        List<PathPoint> points, 
        List<(int rowPos, int colPos)> stationsOrder,
        string pathColor
        )
    {
        (bool isCorrect, Result<RgvMap>? value) = CheckDimensionCorrectness(rowDim, colDim);
        if (!isCorrect && value is not null)
        {
            return value;
        }

        (isCorrect, value) = CheckActualDimensionCorrectness(widthLength, heightLength, stationsOrder);
        if (!isCorrect && value is not null)
        {
            return value;
        }

        PathPoint[,] mapMatrix = CreateMapMatrix(rowDim, colDim);

        (isCorrect, value) = InsertPointsToMatrix(rowDim, colDim, points, mapMatrix);
        if (!isCorrect && value is not null)
        {
            return value;
        }

        List<PathPoint> stationOrderPoints = ToPathPointList(stationsOrder, mapMatrix);

        return new RgvMap(rowDim, colDim, widthLength, heightLength, stationOrderPoints, mapMatrix, [], pathColor);
    }

    public static Result<RgvMap> Create(
        int rowDim,
        int colDim, 
        int widthLength, 
        int heightLength, 
        List<PathPoint> points, 
        List<(int rowPos, int colPos)> stationsOrder,
        string pathColor,
        List<(int rowPos, int colPos)> solution
        )
    {
        (bool isCorrect, Result<RgvMap>? value) = CheckDimensionCorrectness(rowDim, colDim);
        if (!isCorrect && value is not null)
        {
            return value;
        }

        (isCorrect, value) = CheckActualDimensionCorrectness(widthLength, heightLength, stationsOrder);
        if (!isCorrect && value is not null)
        {
            return value;
        }

        PathPoint[,] mapMatrix = CreateMapMatrix(rowDim, colDim);

        (isCorrect, value) = InsertPointsToMatrix(rowDim, colDim, points, mapMatrix);
        if (!isCorrect && value is not null)
        {
            return value;
        }

        List<PathPoint> stationOrderPoints = ToPathPointList(stationsOrder, mapMatrix);
        List<PathPoint> solutionPoints = ToPathPointList(solution, mapMatrix);

        return new RgvMap(rowDim, colDim, widthLength, heightLength, stationOrderPoints, mapMatrix, solutionPoints, pathColor);
    }

    private static List<PathPoint> ToPathPointList(List<(int rowPos, int colPos)> pointLocations, PathPoint[,] mapMatrix)
    {
        List<PathPoint> pathPointList = [];

        foreach (var (rowPos, colPos) in pointLocations)
        {
            pathPointList.Add(mapMatrix[rowPos, colPos]);
        }

        return pathPointList;
    }

    private static (bool isCorrect, Result<RgvMap>? value) InsertPointsToMatrix(int rowDim, int colDim, List<PathPoint> points, PathPoint[,] mapMatrix)
    {
        foreach (PathPoint p in points)
        {
            if (p.RowPos < 0 || p.RowPos >= rowDim)
            {
                return (isCorrect: false, value: Result.Fail<RgvMap>(new InvalidRowPosValueError(p.RowPos, rowDim)));
            }
            if (p.ColPos < 0 || p.ColPos >= colDim)
            {
                return (isCorrect: false, value: Result.Fail<RgvMap>(new InvalidColPosValueError(p.ColPos, colDim)));
            }

            mapMatrix[p.RowPos, p.ColPos] = p;
        }

        return (isCorrect: true, value: null);
    }

    private static PathPoint[,] CreateMapMatrix(int rowDim, int colDim)
    {
        var mapMatrix = new PathPoint[rowDim, colDim];

        for (int rowPos = 0; rowPos < rowDim; rowPos++)
        {
            for (int colPos = 0; colPos < colDim; colPos++)
            {
                mapMatrix[rowPos, colPos] = PathPoint.Path(rowPos, colPos);
            }

        }

        return mapMatrix;
    }

    private static (bool isCorrect, Result<RgvMap>? value) CheckActualDimensionCorrectness(int widthLength, int heightLength, List<(int rowPos, int colPos)> stationsOrder)
    {
        if (widthLength < MinWidthLength || heightLength < MinHeightLength)
        {
            return (isCorrect: false, value: Result.Fail<RgvMap>(new InvalidRgvMapActualDimensionError()));
        }

        if (stationsOrder.Count < MinNumberStationsOrder)
        {
            return (isCorrect: false, value: Result.Fail<RgvMap>(new InvalidNumberOfStationsOrderError()));
        }

        return (isCorrect: true, value: null);
    }

    private static (bool isCorrect, Result<RgvMap>? value) CheckDimensionCorrectness(int rowDim, int colDim)
    {
        if (rowDim < MinRowDim || colDim < MinColDim)
        {
            return (isCorrect: false, value: Result.Fail<RgvMap>(new InvalidRgvMapDimensionError()));
        }

        return (isCorrect: true, value: null);
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
        yield return StationsOrder;
        yield return Solution; 
        yield return MapMatrix;
    }
}

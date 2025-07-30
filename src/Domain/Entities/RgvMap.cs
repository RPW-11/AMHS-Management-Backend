using Domain.Errors.Mission.RoutePlanning;
using Domain.ValueObjects.Mission.RoutePlanning;
using FluentResults;

namespace Domain.Entities;

public class RgvMap
{
    private const int MinRowDim = 3;
    private const int MinColDim = 3;
    private const int MinNumberStationsOrder = 2;
    public static class MapTrajectory
    {
        public static readonly int[] Up = [1, 0];
        public static readonly int[] Down = [-1, 0];
        public static readonly int[] Left = [0, -1];
        public static readonly int[] Right = [0, 1];
        public static readonly int[] UpLeft = [-1, 1];
        public static readonly int[] UpRight = [1, 1];
        public static readonly int[] DownLeft = [-1, -1];
        public static readonly int[] DownRight = [1, -1];
        public static readonly int[][] AllDirections =
        [
            Up, Down, Left, Right,
            UpLeft, UpRight, DownLeft, DownRight
        ];
    }
    public int RowDim { get; private set; }
    public int ColDim { get; private set; }
    public List<PathPoint> StationsOrder { get; }
    public List<PathPoint> Solutions { get; private set; }
    private readonly PathPoint[,] _mapMatrix;

    private RgvMap(int rowDim, int colDim, List<PathPoint> stationsOrder, PathPoint[,] mapMatrix)
    {
        RowDim = rowDim;
        ColDim = colDim;
        StationsOrder = stationsOrder;
        Solutions = [];
        _mapMatrix = mapMatrix;
    }

    public static Result<RgvMap> Create(int rowDim, int colDim, List<PathPoint> points, List<(int rowPos, int colPos)> stationsOrder)
    {
        if (rowDim < MinRowDim || colDim < MinColDim)
        {
            return Result.Fail<RgvMap>(new InvalidRgvMapDimensionError());
        }
        if (stationsOrder.Count < MinNumberStationsOrder)
        {
            return Result.Fail<RgvMap>(new InvalidNumberOfStationsOrderError());
        }

        var mapMatrix = new PathPoint[rowDim, colDim];

        for (int rowPos = 0; rowPos < rowDim; rowPos++)
        {
            for (int colPos = 0; colPos < colDim; colPos++)
            {
                mapMatrix[rowPos, colPos] = PathPoint.Path(rowPos, colPos);
            }

        }

        foreach (PathPoint p in points)
        {
            if (p.RowPos < 0 || p.RowPos >= rowDim)
            {
                return Result.Fail<RgvMap>(new InvalidRowPosValueError(p.RowPos, rowDim));
            }
            if (p.ColPos < 0 || p.ColPos >= colDim)
            {
                return Result.Fail<RgvMap>(new InvalidColPosValueError(p.ColPos, colDim));
            }

            mapMatrix[p.RowPos, p.ColPos] = p;
        }

        List<PathPoint> solutionPointsOrder = [];

        foreach (var (rowPos, colPos) in stationsOrder)
        {
            solutionPointsOrder.Add(mapMatrix[rowPos, colPos]);
        }

        return new RgvMap(rowDim, colDim, solutionPointsOrder, mapMatrix);
    }

    public PathPoint? GetPointAt(int rowPos, int colPos)
    {
        if (rowPos < 0 || rowPos > RowDim - 1 || colPos < 0 || colPos > ColDim - 1)
        {
            return null;
        }

        return _mapMatrix[rowPos, colPos];
    }

    public void SetMapSolution(List<PathPoint> solutions)
    {
        Solutions = solutions;
    }
}

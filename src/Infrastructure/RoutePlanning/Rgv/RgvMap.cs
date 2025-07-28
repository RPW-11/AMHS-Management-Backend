using Domain.ValueObjects.Mission.RoutePlanning;

namespace Infrastructure.RoutePlanning.Rgv;

internal static class MapTrajectory
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

internal class RgvMap
{
    public int RowDim { get; private set; }
    public int ColDim { get; private set; }
    public List<PathPoint> StationsOrder;
    private readonly PathPoint[,] _mapMatrix;

    private RgvMap(int rowDim, int colDim, List<PathPoint> stationsOrder, PathPoint[,] mapMatrix)
    {
        RowDim = rowDim;
        ColDim = colDim;
        StationsOrder = stationsOrder;
        _mapMatrix = mapMatrix;
    }

    public static RgvMap Create(int rowDim, int colDim, List<(int rowPos, int colPos)> stationsOrder, List<PathPoint> points)
    {
        if (rowDim < 3 || colDim < 3)
        {
            throw new Exception("Invalid row dimension or column dimension (each must be at least a value of 3)");
        }

        if (stationsOrder.Count < 2)
        {
            throw new Exception("Stations order must be more be at least 2 (start -> goal)");
        }

        PathPoint[,] mapMatrix = new PathPoint[rowDim, colDim];

        for (int rowPos = 0; rowPos < rowDim; rowPos++)
        {
            for (int colPos = 0; colPos < colDim; colPos++)
            {
                mapMatrix[rowPos, colPos] = PathPoint.Create("", "", rowPos, colPos, 0).Value; // must be correct
            }
            
        }

        foreach (PathPoint p in points)
            {
                if (p.RowPos < 0 || p.RowPos >= rowDim)
                {
                    throw new Exception($"Invalid row value of (${p.RowPos}) with a dimension of ${rowDim}");
                }
                if (p.ColPos < 0 || p.ColPos >= colDim)
                {
                    throw new Exception($"Invalid column value of (${p.ColPos}) with a dimension of ${colDim}");
                }
                mapMatrix[p.RowPos, p.ColPos] = p;
            }

        List<PathPoint> solutionPointsOrder = [];

        foreach (var (rowPos, colPos) in stationsOrder)
        {
            solutionPointsOrder.Add(mapMatrix[rowPos, colPos]);
        }

        return new(rowDim, colDim, solutionPointsOrder, mapMatrix);
    }

    public PathPoint? GetPointAt(int rowPos, int colPos)
    {
        if (rowPos < 0 || rowPos > RowDim - 1 || colPos < 0 || colPos > ColDim - 1)
        {
            return null;
        }

        return _mapMatrix[rowPos, colPos];
    }
}

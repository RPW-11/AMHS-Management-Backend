using Application.Common.Interfaces.RoutePlanning;
using Domain.ValueObjects.Mission.RoutePlanning;

namespace Infrastructure.RoutePlanning.Rgv;

public class RgvRoutePlanning : IRgvRoutePlanning
{
    public byte[] DrawImage(byte[] originalImage, IEnumerable<(int rowPos, int colPos)> coordinates, int rowDim, int colDim)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<PathPoint> Solve(int rowDim, int colDim, IEnumerable<PathPoint> points, IEnumerable<(int rowPos, int colPos)> stationsOrder)
    {
        
        RgvMap rgvMap = RgvMap.Create(rowDim, colDim, [.. stationsOrder], [.. points]);

        var route = DfsSolver.FindBestRoute(rgvMap);
        foreach (var p in route)
        {
            Console.Write($"({p.RowPos},{p.ColPos})->");
        }
        Console.WriteLine("");

        return route;
    }
}

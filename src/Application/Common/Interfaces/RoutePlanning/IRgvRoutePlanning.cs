using Domain.ValueObjects.Mission.RoutePlanning;

namespace Application.Common.Interfaces.RoutePlanning;

public interface IRgvRoutePlanning
{
    IEnumerable<PathPoint> Solve(int rowDim, int colDim, IEnumerable<PathPoint> points, IEnumerable<(int rowPos, int colPos)> stationsOrder);
    byte[] DrawImage(byte[] originalImage, IEnumerable<(int rowPos, int colPos)> coordinates, int rowDim, int colDim);
}

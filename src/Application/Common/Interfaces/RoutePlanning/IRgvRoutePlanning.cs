using Domain.Mission.ValueObjects;

namespace Application.Common.Interfaces.RoutePlanning;

public interface IRgvRoutePlanning
{
    IEnumerable<PathPoint> Solve(RgvMap rgvMap);
    void DrawImage(MemoryStream imageStream, RgvMap rgvMap);
}

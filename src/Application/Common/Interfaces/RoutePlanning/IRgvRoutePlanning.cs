using Domain.Entities;
using Domain.ValueObjects.Mission.RoutePlanning;

namespace Application.Common.Interfaces.RoutePlanning;

public interface IRgvRoutePlanning
{
    IEnumerable<PathPoint> Solve(RgvMap rgvMap);
    void DrawImage(MemoryStream imageStream, RgvMap rgvMap);
}

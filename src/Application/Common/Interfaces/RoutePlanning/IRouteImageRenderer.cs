using Domain.Missions.ValueObjects;

namespace Application.Common.Interfaces.RoutePlanning;

public interface IRouteImageRenderer
{
    byte[] Render(byte[] imageBytes, Grid grid, List<(List<PathPoint> Solution, string ArrowColor)> routes);
}

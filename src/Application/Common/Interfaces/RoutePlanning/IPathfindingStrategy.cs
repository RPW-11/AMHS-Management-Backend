using Domain.Missions.ValueObjects;

namespace Application.Common.Interfaces.RoutePlanning;

public interface IPathfindingStrategy
{
    RoutePlanningAlgorithm Algorithm { get; }

    IEnumerable<PathPoint> Solve(
        Grid grid,
        List<PathPoint> stationsOrder,
        List<List<PathPoint>> currentRoutePoints,
        RouteSolvePurpose purpose);
}

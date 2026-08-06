using Domain.Missions.ValueObjects;

namespace Application.Common.Interfaces.RoutePlanning;

public interface IPathfindingStrategy
{
    RoutePlanningAlgorithm Algorithm { get; }

    /// <summary>
    /// Solves the route through <paramref name="stationsOrder"/>, returning up to
    /// <paramref name="desiredSolutions"/> distinct candidates ordered best first. Callers must
    /// cope with fewer being returned; only the first is guaranteed.
    /// </summary>
    IReadOnlyList<List<PathPoint>> Solve(
        Grid grid,
        List<PathPoint> stationsOrder,
        List<List<PathPoint>> currentRoutePoints,
        RouteSolvePurpose purpose,
        int desiredSolutions);
}

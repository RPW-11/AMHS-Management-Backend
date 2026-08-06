using Domain.Missions.ValueObjects;

namespace Application.Common.Interfaces.RoutePlanning;

public interface IPathfindingStrategy
{
    RoutePlanningAlgorithm Algorithm { get; }

    /// <summary>
    /// Solves the requested route, returning up to <see cref="RouteSolveRequest.DesiredSolutions"/>
    /// distinct candidates ordered best first. Callers must cope with fewer being returned; only
    /// the first is guaranteed.
    /// </summary>
    IReadOnlyList<List<PathPoint>> Solve(RouteSolveRequest request);
}

using Application.Common.Interfaces.RoutePlanning;
using Domain.Missions.ValueObjects;

namespace Application.Services.RoutePlanningService;

public interface IClusterFlowRouteSolver
{
    List<PathPoint> SolveClusterRoute(Grid grid, Cluster cluster, IPathfindingStrategy strategy, List<List<PathPoint>> currentRoutes);

    /// <summary>
    /// Solves the connector between two adjacent clusters, returning up to
    /// <paramref name="desiredSolutions"/> distinct routes ordered best first. At least one route
    /// is always returned.
    /// </summary>
    IReadOnlyList<List<PathPoint>> SolveConnectorRoutes(
        Grid grid,
        Cluster from,
        Cluster to,
        IPathfindingStrategy strategy,
        List<List<PathPoint>> currentRoutes,
        int desiredSolutions = 1,
        IReadOnlyList<List<PathPoint>>? seedPaths = null);
}

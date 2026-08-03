using Application.Common.Interfaces.RoutePlanning;
using Domain.Missions.ValueObjects;

namespace Application.Services.RoutePlanningService;

public interface IClusterFlowRouteSolver
{
    List<PathPoint> SolveClusterRoute(Grid grid, Cluster cluster, IPathfindingStrategy strategy, List<List<PathPoint>> currentRoutes);

    List<PathPoint> SolveConnectorRoute(Grid grid, Cluster from, Cluster to, IPathfindingStrategy strategy, List<List<PathPoint>> currentRoutes);
}

using Domain.Missions.ValueObjects;

namespace Application.Services.RoutePlanningService;

public interface IClusterFlowRouteSolver
{
    List<PathPoint> SolveClusterRoute(Grid grid, Cluster cluster, RoutePlanningAlgorithm algorithm, List<List<PathPoint>> currentRoutes);

    List<PathPoint> SolveConnectorRoute(Grid grid, Cluster from, Cluster to, RoutePlanningAlgorithm algorithm, List<List<PathPoint>> currentRoutes);
}

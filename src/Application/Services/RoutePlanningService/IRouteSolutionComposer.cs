using Application.Common.Interfaces.RoutePlanning;
using Domain.Missions.ValueObjects;

namespace Application.Services.RoutePlanningService;

public interface IRouteSolutionComposer
{
    /// <summary>
    /// Produces up to <paramref name="desiredSolutions"/> complete routings of the map, best score
    /// first. Fewer are returned when the layout admits fewer - a map whose every flow is a single
    /// cluster has no connectors and so exactly one routing.
    /// </summary>
    IReadOnlyList<ComposedSolution> Compose(
        RgvMap rgvMap,
        IPathfindingStrategy strategy,
        ClusterLoopSolutions clusterLoops,
        int desiredSolutions);
}

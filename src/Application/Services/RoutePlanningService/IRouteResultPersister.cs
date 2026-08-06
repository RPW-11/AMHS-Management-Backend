using Application.DTOs.RoutePlanning;
using Domain.Missions;
using Domain.Missions.ValueObjects;

namespace Application.Services.RoutePlanningService;

public interface IRouteResultPersister
{
    /// <summary>
    /// Draws and stores every composed routing, then the document describing them, then finishes
    /// the mission.
    /// </summary>
    /// <param name="solutions">The routings to store, best first.</param>
    Task PersistAsync(
        MissionBase mission,
        Grid grid,
        RoutePlanningAlgorithm algorithm,
        byte[] imageBytes,
        IReadOnlyList<ComposedSolution> solutions,
        RgvMapDetailDto rgvMap,
        IEnumerable<ClusterDefinitionDto> clusters,
        IEnumerable<ClusterFlowDefinitionDto> clusterFlows,
        CancellationToken cancellationToken = default);
}

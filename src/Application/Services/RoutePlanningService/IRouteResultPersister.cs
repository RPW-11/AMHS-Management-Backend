using Application.DTOs.RoutePlanning;
using Domain.Missions;
using Domain.Missions.ValueObjects;

namespace Application.Services.RoutePlanningService;

public interface IRouteResultPersister
{
    Task PersistAsync(
        MissionBase mission,
        Grid grid,
        RoutePlanningAlgorithm algorithm,
        byte[] imageBytes,
        List<(List<PathPoint> Solution, string ArrowColor)> routes,
        RgvMapDetailDto rgvMap,
        IEnumerable<ClusterDefinitionDto> clusters,
        IEnumerable<ClusterFlowDefinitionDto> clusterFlows,
        IEnumerable<ClusterFlowSolutionDto> routeSolutions,
        RoutePlanningScoreDto score,
        CancellationToken cancellationToken = default);
}

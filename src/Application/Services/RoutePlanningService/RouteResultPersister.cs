using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.RoutePlanning;
using Domain.Missions;
using Domain.Missions.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Services.RoutePlanningService;

public class RouteResultPersister(IRoutePlanningResultStore routePlanningResultStore, ILogger<RouteResultPersister> logger) : IRouteResultPersister
{
    private readonly IRoutePlanningResultStore _routePlanningResultStore = routePlanningResultStore;
    private readonly ILogger<RouteResultPersister> _logger = logger;

    public void Persist(
        MissionBase mission,
        Grid grid,
        RoutePlanningAlgorithm algorithm,
        byte[] imageBytes,
        List<(List<PathPoint> Solution, string ArrowColor)> routes,
        RgvMapDetailDto rgvMap,
        IEnumerable<ClusterDefinitionDto> clusters,
        IEnumerable<ClusterFlowDefinitionDto> clusterFlows,
        IEnumerable<ClusterFlowSolutionDto> routeSolutions,
        RoutePlanningScoreDto score)
    {
        string missionId = mission.Id.ToString();

        var drawnImageBytes = _routePlanningResultStore.DrawMultipleFlows(imageBytes, grid, routes);

        var inputImagePath = _routePlanningResultStore.WriteImage(imageBytes, missionId, RouteImageKind.Input);
        var imagePath = _routePlanningResultStore.WriteImage(drawnImageBytes, missionId, RouteImageKind.Solved);

        var routePlanningDetail = ToRoutePlanningDto(mission.Id, algorithm, inputImagePath, [imagePath], rgvMap, clusters, clusterFlows, routeSolutions, score);

        _routePlanningResultStore.SaveRoutePlanningDetail(routePlanningDetail);
        _logger.LogInformation("Route planning data saved for mission {MissionId}", mission.Id);

        mission.Finish();
    }

    private static RoutePlanningDetailDto ToRoutePlanningDto(
        MissionId missionId,
        RoutePlanningAlgorithm routePlanningAlgorithm,
        string inputImageUrl,
        List<string> imageUrls,
        RgvMapDetailDto rgvMap,
        IEnumerable<ClusterDefinitionDto> clusters,
        IEnumerable<ClusterFlowDefinitionDto> clusterFlows,
        IEnumerable<ClusterFlowSolutionDto> routeSolutions,
        RoutePlanningScoreDto score)
    {
        return new(
                    missionId.ToString(),
                    routePlanningAlgorithm.ToString(),
                    inputImageUrl,
                    imageUrls,
                    rgvMap,
                    clusters,
                    clusterFlows,
                    routeSolutions,
                    score
                );
    }
}

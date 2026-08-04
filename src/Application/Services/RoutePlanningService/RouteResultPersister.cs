using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.RoutePlanning;
using Domain.Missions;
using Domain.Missions.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Services.RoutePlanningService;

public class RouteResultPersister(
    IRoutePlanningResultStore routePlanningResultStore,
    IRouteImageRenderer routeImageRenderer,
    ILogger<RouteResultPersister> logger) : IRouteResultPersister
{
    private readonly IRoutePlanningResultStore _routePlanningResultStore = routePlanningResultStore;
    private readonly IRouteImageRenderer _routeImageRenderer = routeImageRenderer;
    private readonly ILogger<RouteResultPersister> _logger = logger;

    public async Task PersistAsync(
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
        CancellationToken cancellationToken = default)
    {
        string missionId = mission.Id.ToString();

        var drawnImageBytes = _routeImageRenderer.Render(imageBytes, grid, routes);

        var inputImagePath = await _routePlanningResultStore.WriteImageAsync(imageBytes, missionId, RouteImageKind.Input, cancellationToken);
        var imagePath = await _routePlanningResultStore.WriteImageAsync(drawnImageBytes, missionId, RouteImageKind.Solved, cancellationToken);

        var routePlanningDetail = ToRoutePlanningDto(mission.Id, algorithm, inputImagePath, [imagePath], rgvMap, clusters, clusterFlows, routeSolutions, score);

        await _routePlanningResultStore.SaveRoutePlanningDetailAsync(routePlanningDetail, cancellationToken);
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

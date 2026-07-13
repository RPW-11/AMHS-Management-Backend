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
        IEnumerable<ClusterFlowSolutionDto> routeSolutions,
        RoutePlanningScoreDto score)
    {
        var drawnImageBytes = _routePlanningResultStore.DrawMultipleFlows(imageBytes, grid, routes);
        var imagePath = _routePlanningResultStore.WriteImage(drawnImageBytes, mission.Id.ToString());

        var routePlanningDetail = ToRoutePlanningDto(mission.Id, algorithm, [imagePath], rgvMap, routeSolutions, score);

        _routePlanningResultStore.SaveRoutePlanningDetail(routePlanningDetail);
        _logger.LogInformation("Route planning data saved for mission {MissionId}", mission.Id);

        mission.Finish();
    }

    private static RoutePlanningDetailDto ToRoutePlanningDto(
        MissionId missionId,
        RoutePlanningAlgorithm routePlanningAlgorithm,
        List<string> imageUrls,
        RgvMapDetailDto rgvMap,
        IEnumerable<ClusterFlowSolutionDto> routeSolutions,
        RoutePlanningScoreDto score)
    {
        return new(
                    missionId.ToString(),
                    routePlanningAlgorithm.ToString(),
                    imageUrls,
                    rgvMap,
                    routeSolutions,
                    score
                );
    }
}

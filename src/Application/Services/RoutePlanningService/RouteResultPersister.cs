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
        MemoryStream imageStream,
        List<(List<PathPoint> Solution, string ArrowColor)> routes,
        RgvMapDetailDto rgvMap,
        RoutePlanningScoreDto score)
    {
        try
        {
            var drawnImageBytes = _routePlanningResultStore.DrawMultipleFlows(imageStream.ToArray(), grid, routes);
            var imagePath = _routePlanningResultStore.WriteImage(drawnImageBytes, mission.Id.ToString());

            var routePlanningDetail = ToRoutePlanningDto(mission.Id, algorithm, [imagePath], rgvMap, score);

            _routePlanningResultStore.SaveRoutePlanningDetail(routePlanningDetail);
            _logger.LogInformation("Route planning data saved for mission {MissionId}", mission.Id);

            mission.Finish();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to draw, write image, or write route planning JSON result");
            mission.SetMissionStatus(MissionStatus.Failed);
        }
    }

    private static RoutePlanningDetailDto ToRoutePlanningDto(
        MissionId missionId,
        RoutePlanningAlgorithm routePlanningAlgorithm,
        List<string> imageUrls,
        RgvMapDetailDto rgvMap,
        RoutePlanningScoreDto score)
    {
        return new(
                    missionId.ToString(),
                    routePlanningAlgorithm.ToString(),
                    imageUrls,
                    rgvMap,
                    score
                );
    }
}

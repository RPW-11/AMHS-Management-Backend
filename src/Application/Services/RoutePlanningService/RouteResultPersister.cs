using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.RoutePlanning;
using Domain.Missions;
using Domain.Missions.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Services.RoutePlanningService;

public class RouteResultPersister(IRgvRoutePlanning rgvRoutePlanning, ILogger<RouteResultPersister> logger) : IRouteResultPersister
{
    private readonly IRgvRoutePlanning _rgvRoutePlanning = rgvRoutePlanning;
    private readonly ILogger<RouteResultPersister> _logger = logger;

    public void Persist(
        MissionBase mission,
        Grid grid,
        RoutePlanningAlgorithm algorithm,
        MemoryStream imageStream,
        List<(List<PathPoint> Solution, string ArrowColor)> routes,
        RouteSolutionDto routeSolution)
    {
        try
        {
            var drawnImageBytes = _rgvRoutePlanning.DrawMultipleFlows(imageStream.ToArray(), grid, routes);
            var imagePath = _rgvRoutePlanning.WriteImage(drawnImageBytes, mission.Id.ToString());

            var routePlanningDetail = ToRoutePlanningDto(mission.Id, algorithm, [imagePath], routeSolution);

            var resourceLink = _rgvRoutePlanning.WriteToJson(routePlanningDetail);
            _logger.LogInformation("Route planning data saved to JSON: {ResourceLink}", resourceLink);

            mission.Finish();
            mission.SetMissionResourceLink(resourceLink);
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
        RouteSolutionDto routeSolution)
    {
        return new(
                    missionId.ToString(),
                    routePlanningAlgorithm.ToString(),
                    imageUrls,
                    routeSolution
                );
    }
}

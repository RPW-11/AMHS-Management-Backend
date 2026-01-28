using Application.DTOs.Mission.RoutePlanning;
using FluentResults;

namespace Application.Services.MissionService.RoutePlanningService;

public interface IRoutePlanningService
{
    Task<Result> FindRgvBestRoute(
        string missionId,
        MemoryStream imageSteam,
        string algorithm,
        int rowDim,
        int colDim,
        int widthLength,
        int heightLength,
        IEnumerable<PathPointDto> points,
        IEnumerable<RouteFlowDto> routeFlows,
        IEnumerable<IEnumerable<PointPositionDto>> sampleSolutions
    );
}

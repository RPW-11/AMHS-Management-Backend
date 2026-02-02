using Application.DTOs.RoutePlanning;
using FluentResults;

namespace Application.Services.RoutePlanningService;

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
        IEnumerable<RouteFlowDto> routeFlows
    );
}

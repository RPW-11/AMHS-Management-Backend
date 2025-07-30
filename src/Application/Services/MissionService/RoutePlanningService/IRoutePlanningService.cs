using Application.DTOs.Mission.RoutePlanning;
using FluentResults;

namespace Application.Services.MissionService.RoutePlanningService;

public interface IRoutePlanningService
{
    Result FindRgvBestRoute(
        MemoryStream imageSteam,
        int rowDim,
        int colDim,
        IEnumerable<PathPointDto> points,
        IEnumerable<PointPositionDto> stationsOrder
    );
}

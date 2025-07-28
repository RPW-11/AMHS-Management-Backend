using Application.DTOs.Mission.RoutePlanning;
using FluentResults;

namespace Application.Services.TaskService.RoutePlanningService;

public interface IRoutePlanningService
{
    Result FindRgvBestRoute(int rowDim, int colDim, IEnumerable<PathPointDto> points, IEnumerable<PointPositionDto> stationsOrder);
}

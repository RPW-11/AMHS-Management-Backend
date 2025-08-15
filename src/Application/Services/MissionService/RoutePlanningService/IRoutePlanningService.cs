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
        IEnumerable<PathPointDto> points,
        IEnumerable<PointPositionDto> stationsOrder,
        IEnumerable<IEnumerable<PointPositionDto>> sampleSolutions
    );
}

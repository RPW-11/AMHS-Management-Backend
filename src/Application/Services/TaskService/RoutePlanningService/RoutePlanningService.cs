using Application.Common.Errors;
using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.Mission.RoutePlanning;
using Domain.ValueObjects.Mission.RoutePlanning;
using FluentResults;

namespace Application.Services.TaskService.RoutePlanningService;

public class RoutePlanningService : IRoutePlanningService
{
    private readonly IRgvRoutePlanning _rgvRoutePlanning;

    public RoutePlanningService(IRgvRoutePlanning rgvRoutePlanning)
    {
        _rgvRoutePlanning = rgvRoutePlanning;
    }

    public Result FindRgvBestRoute(MemoryStream imageStream, int rowDim, int colDim, IEnumerable<PathPointDto> points, IEnumerable<PointPositionDto> stationsOrder)
    {
        List<PathPoint> pathPoints = [];

        foreach (var point in points) {
            var pathPointResult = PathPoint.Create(
                point.Name,
                point.Category,
                point.Position.RowPos,
                point.Position.ColPos,
                point.Time
            );

            if (pathPointResult.IsFailed) {
                return Result.Fail(new ApplicationError(pathPointResult.Errors[0].Message, "FindRgvBestRoute.InvalidPoint"));
            }

            pathPoints.Add(pathPointResult.Value);
        }

        List<(int rowPos, int colPos)> stationOrderPoints = [];

        foreach (var station in stationsOrder) {
            stationOrderPoints.Add((station.RowPos, station.ColPos));
        }

        try
        {
            var routes = _rgvRoutePlanning.Solve(
                rowDim,
                colDim,
                pathPoints,
                stationOrderPoints
            );

            List<(int, int)> coordinates = [];

            foreach (var route in routes)
            {
                coordinates.Add((route.RowPos, route.ColPos));
            }

            // draw next
            _rgvRoutePlanning.DrawImage(
                imageStream,
                coordinates,
                rowDim,
                colDim
            );

            return Result.Ok();
        }
        catch (Exception error)
        {
            return Result.Fail(new ApplicationError(error.Message, "FindRgvBestRoute.ServerError"));
        }
    }
}

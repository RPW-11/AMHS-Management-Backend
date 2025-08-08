using Application.Common.Errors;
using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.Mission.RoutePlanning;
using Domain.Mission.ValueObjects;
using FluentResults;

namespace Application.Services.MissionService.RoutePlanningService;

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
                return Result.Fail(ApplicationError.Validation(pathPointResult.Errors[0].Message));
            }

            pathPoints.Add(pathPointResult.Value);
        }

        List<(int rowPos, int colPos)> stationOrderPoints = [];

        foreach (var station in stationsOrder) {
            stationOrderPoints.Add((station.RowPos, station.ColPos));
        }

        // map creation
        var mapResult = RgvMap.Create(
            rowDim,
            colDim,
            pathPoints,
            stationOrderPoints
        );

        if (mapResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation(mapResult.Errors[0].Message));
        }

        RgvMap rgvMap = mapResult.Value;

        // Write the map file to json.
        _rgvRoutePlanning.WriteToJson(rgvMap);

        var routes = _rgvRoutePlanning.Solve(rgvMap);

        if (!routes.Any())
        {
            return Result.Fail(ApplicationError.NotFound("No solution is found"));
        }

        rgvMap.SetMapSolution([.. routes]);

        _rgvRoutePlanning.WriteToJson(rgvMap);
        
        // draw next
        _rgvRoutePlanning.DrawImage(
            imageStream,
            rgvMap
        );

        return Result.Ok();
    }
}

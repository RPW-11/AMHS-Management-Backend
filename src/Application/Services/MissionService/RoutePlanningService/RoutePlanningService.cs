using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.Mission.RoutePlanning;
using Domain.Mission;
using Domain.Mission.ValueObjects;
using FluentResults;

namespace Application.Services.MissionService.RoutePlanningService;

public class RoutePlanningService : BaseService, IRoutePlanningService
{
    private readonly IRgvRoutePlanning _rgvRoutePlanning;
    private readonly IMissionRepository _missionRepository;

    public RoutePlanningService(IRgvRoutePlanning rgvRoutePlanning, IMissionRepository missionRepository, IUnitOfWork unitOfWork)
    : base(unitOfWork)
    {
        _rgvRoutePlanning = rgvRoutePlanning;
        _missionRepository = missionRepository;
    }

    public async Task<Result> FindRgvBestRoute(string missionId,
                                   MemoryStream imageStream,
                                   string algorithm,
                                   int rowDim,
                                   int colDim,
                                   int widthLength,
                                   int heightLength,
                                   IEnumerable<PathPointDto> points,
                                   IEnumerable<PointPositionDto> stationsOrder,
                                   IEnumerable<IEnumerable<PointPositionDto>> sampleSolutions)
    {
        // Validate whether the mission exists or not and its category
        var missionIdResult = MissionId.FromString(missionId);
        if (missionIdResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation(missionIdResult.Errors[0].Message));
        }

        var missionResult = await _missionRepository.GetMissionByIdAsync(missionIdResult.Value);
        if (missionResult.Value is null)
        {
            return Result.Fail(ApplicationError.NotFound("Mission is not found"));
        }

        if (missionResult.Value.Category != MissionCategory.RoutePlanning)
        {
            return Result.Fail(ApplicationError.Validation("The selected mission is not a route-planning mission"));
        }

        List<PathPoint> pathPoints = [];

        foreach (var point in points)
        {
            var pathPointResult = PathPoint.Create(
                point.Name,
                point.Category,
                point.Position.RowPos,
                point.Position.ColPos,
                point.Time
            );

            if (pathPointResult.IsFailed)
            {
                return Result.Fail(ApplicationError.Validation(pathPointResult.Errors[0].Message));
            }

            pathPoints.Add(pathPointResult.Value);
        }

        List<(int rowPos, int colPos)> stationOrderPoints = [];

        foreach (var station in stationsOrder)
        {
            stationOrderPoints.Add((station.RowPos, station.ColPos));
        }

        List<List<PathPoint>> convertedSampleSolutions = [];

        foreach (var sol in sampleSolutions)
        {
            List<PathPoint> convertedSolution = [];
            foreach (var point in sol)
            {
                convertedSolution.Add(PathPoint.Path(point.RowPos, point.ColPos));
            }
            convertedSampleSolutions.Add(convertedSolution);
        } 

        // map creation
        var mapResult = RgvMap.Create(
            rowDim,
            colDim,
            widthLength,
            heightLength,
            pathPoints,
            stationOrderPoints
        );

        if (mapResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation(mapResult.Errors[0].Message));
        }

        RgvMap rgvMap = mapResult.Value;

        // Algorithm correctness check
        var algorithmResult = RoutePlanningAlgorithm.FromString(algorithm);
        if (algorithmResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation(algorithmResult.Errors[0].Message));
        }

        // Modify the current mission
        RoutePlanningMission routePlanningMission = RoutePlanningMission.FromBaseClass(missionResult.Value,
                                                                                       "Not Configured Yet",
                                                                                       algorithmResult.Value,
                                                                                       rgvMap);

        // Convert to RoutePlanningSummary
        var routePlanningDetail = ToRoutePlanningDto(routePlanningMission);

        // Write the map file to json.
        string resourceLink = _rgvRoutePlanning.WriteToJson(routePlanningDetail);

        var routes = _rgvRoutePlanning.Solve(routePlanningDetail, algorithmResult.Value, convertedSampleSolutions);

        if (!routes.Any())
        {
            return Result.Fail(ApplicationError.NotFound("No solution is found"));
        }

        // Get the route scores
        var scores = _rgvRoutePlanning.GetRouteScore([.. routes], rgvMap);

        rgvMap.SetMapSolution([.. routes]);

        string imgResultLink = _rgvRoutePlanning.DrawImage(
            imageStream,
            routePlanningDetail
        );
        routePlanningMission.SetMissionResourceLink(resourceLink);
        routePlanningMission.SetImageUrl(imgResultLink);
        routePlanningDetail = ToRoutePlanningDto(routePlanningMission, scores);

        _rgvRoutePlanning.WriteToJson(routePlanningDetail);

        // Update the mission status to finished. Also, 
        missionResult.Value.SetMissionStatus(MissionStatus.Finished);
        missionResult.Value.SetMissionResourceLink(resourceLink);
        var updateResult = _missionRepository.UpdateMission(missionResult.Value);

        if (updateResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Internal);
        }

        await _unitOfWork.SaveChangesAsync();

        return Result.Ok();
    }

    private static RoutePlanningDetailDto ToRoutePlanningDto(RoutePlanningMission routePlanningMission, RoutePlanningScoreDto? scoreDto = null)
    {
        return new(
                    routePlanningMission.Id.ToString(),
                    routePlanningMission.Algorithm.ToString(),
                    routePlanningMission.ImageUrl,
                    routePlanningMission.RgvMap,
                    scoreDto
                );
    }
}

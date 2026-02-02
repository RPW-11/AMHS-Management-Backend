using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.RoutePlanning;
using Application.Common.Utilities;
using Application.DTOs.RoutePlanning;
using Domain.Missions;
using Domain.Missions.ValueObjects;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Application.Services.RoutePlanningService;

public class RoutePlanningService : BaseService, IRoutePlanningService
{
    private readonly IRgvRoutePlanning _rgvRoutePlanning;
    private readonly IMissionRepository _missionRepository;
    private readonly ILogger<RoutePlanningService> _logger;


    public RoutePlanningService(IRgvRoutePlanning rgvRoutePlanning,
                                IMissionRepository missionRepository,
                                IUnitOfWork unitOfWork,
                                ILogger<RoutePlanningService> logger)
                                : base(unitOfWork)
    {
        _rgvRoutePlanning = rgvRoutePlanning;
        _missionRepository = missionRepository;
        _logger = logger;
    }

    public async Task<Result> FindRgvBestRoute(string missionId,
                                   MemoryStream imageStream,
                                   string algorithm,
                                   int rowDim,
                                   int colDim,
                                   int widthLength,
                                   int heightLength,
                                   IEnumerable<PathPointDto> points,
                                   IEnumerable<RouteFlowDto> routeFlows)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MissionId"] = missionId,
            ["Algorithm"] = algorithm,
            ["GridSize"] = $"{rowDim}×{colDim}",
            ["ActualDimension"] = $"{widthLength}x{heightLength}"
        });

        _logger.LogInformation("Route planning request started | Input points: {PointCount}", points.Count());

        // Validate whether the mission exists or not and its category
        var missionIdResult = MissionId.FromString(missionId);
        if (missionIdResult.IsFailed)
        {
            _logger.LogWarning("Invalid mission ID format: {ErrorMessage}",
                missionIdResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation(missionIdResult.Errors[0].Message));
        }

        var missionResult = await _missionRepository.GetMissionByIdAsync(missionIdResult.Value);
        if (missionResult.Value is null)
        {
            _logger.LogError("Failed to load mission from repository: {ErrorMessage}", missionResult.Errors[0].Message);
            return Result.Fail(ApplicationError.NotFound("Mission is not found"));
        }

        if (missionResult.Value.Category != MissionCategory.RoutePlanning)
        {
            _logger.LogWarning("Mission category mismatch - expected RoutePlanning, got {Category}",
                missionResult.Value.Category);
            return Result.Fail(ApplicationError.Validation("The selected mission is not a route-planning mission"));
        }

        _logger.LogDebug("Mission validated | Category: {Category} | Name: {Name}",
            missionResult.Value.Category, missionResult.Value.Name ?? "(no name)");

        (bool isError, Result? value) = ToPathPoints(points, out List<PathPoint> pathPoints);
        if (isError && value is not null)
        {
            return value;
        }

        // Route flows conversion
        List<List<(int rowPos, int colPos)>> stationsOrders = [];
        foreach (var routeFlow in routeFlows)
        {
            var routeFlowPoints = routeFlow.StationsOrder
                .Select(p => (p.RowPos, p.ColPos))
                .ToList();
            stationsOrders.Add(routeFlowPoints);
        }

        // Maps creation
        List<RgvMap> rgvMaps = [];
        foreach (var stationOrder in stationsOrders)
        {
            var mapResult = RgvMap.Create(
                rowDim,
                colDim,
                widthLength,
                heightLength,
                pathPoints,
                stationOrder
            );

            if (mapResult.IsFailed)
            {
                _logger.LogWarning("Failed to create RGV map: {ErrorMessage}",
                    mapResult.Errors[0].Message);
                return Result.Fail(ApplicationError.Validation(mapResult.Errors[0].Message));
            }

            rgvMaps.Add(mapResult.Value);
        }

        _logger.LogDebug("RGV maps created with count: {MapCount} | Grid size: {RowDim}x{ColDim}",
                rgvMaps.Count, rowDim, colDim);

        var algorithmResult = RoutePlanningAlgorithm.FromString(algorithm);
        if (algorithmResult.IsFailed)
        {
            _logger.LogWarning("Unsupported or invalid algorithm: {Algorithm}: {ErrorMessage}",
                algorithm, algorithmResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation(algorithmResult.Errors[0].Message));
        }

        // Solve for each flow
        List<List<PathPoint>> flowSolutions = [];
        List<List<PathPoint>> postProcessedFlowSolutions = [];
        List<RoutePlanningScoreDto> flowScores = [];
        for (int i = 0; i < routeFlows.Count(); i++)
        {
            var rgvMap = rgvMaps[i];

            var (routes, postProcessedRoutes) = _rgvRoutePlanning.Solve(
                rgvMap,
                flowSolutions,
                algorithmResult.Value
            );

            if (!routes.Any())
            {
                _logger.LogInformation("No route solution found after solving");
                return Result.Fail(ApplicationError.NotFound("No solution is found"));
            }

            _logger.LogInformation("Flow {FlowNumber} -> Route found | Original points: {Count} | Post-processed: {PostCount}",
                i + 1, routes.Count(), postProcessedRoutes.Count());

            var scores = _rgvRoutePlanning.GetRouteScore([.. routes], rgvMap);

            flowSolutions.Add([.. routes]);
            postProcessedFlowSolutions.Add([.. postProcessedRoutes]);
            flowScores.Add(scores);
        }
        

        // Get intersections and draw original solution
        for (int i = 0; i < flowSolutions.Count; i++)
        {
            rgvMaps[i].SetMapSolution(flowSolutions[i]);
        }
        List<PathPoint> intersections = RouteIntersection.GetIntersectionPathPoints(flowSolutions);

        (isError, value) = DrawSolution(imageStream, routeFlows, rgvMaps, intersections, out byte[] drawnImageBytes);
        if (isError && value is not null)
        {
            return value;
        }

        var originalImgUrl = _rgvRoutePlanning.WriteImage(drawnImageBytes, $"{missionId}_original_solution");;
        _logger.LogInformation("Original route image saved at: {ImageUrl}", originalImgUrl);


        // Draw post-processed solution
        for (int i = 0; i < flowSolutions.Count; i++)
        {
            rgvMaps[i].SetMapSolution(postProcessedFlowSolutions[i]);
        }
        List<PathPoint> postProcessedIntersections = RouteIntersection.GetIntersectionPathPoints(postProcessedFlowSolutions);
        (isError, value) = DrawSolution(imageStream, routeFlows, rgvMaps, postProcessedIntersections, out byte[] postProcessedImageBytes);
        if (isError && value is not null)
        {
            return value;
        }
        var postProcessedImgUrl = _rgvRoutePlanning.WriteImage(postProcessedImageBytes, $"{missionId}_postprocessed_solution");;
        _logger.LogInformation("Post-processed route image saved at: {ImageUrl}", postProcessedImgUrl);

        var routePlanningMission = RoutePlanningMission.FromBaseClass(missionResult.Value,
                                                                    algorithmResult.Value,
                                                                    rgvMaps);

        routePlanningMission.AddImageUrl(originalImgUrl);
        routePlanningMission.AddImageUrl(postProcessedImgUrl);

        List<RouteSolutionDto> routeSolutions = [];
        for (int i = 0; i < flowSolutions.Count; i++)
        {
            var routeSolutionDto = new RouteSolutionDto(
                rgvMaps[i],
                flowScores[i]
            );
            routeSolutions.Add(routeSolutionDto);
        }

        var routePlanningDetail = ToRoutePlanningDto(routePlanningMission, routeSolutions);
        string resourceLink;
        try
        {
            resourceLink = _rgvRoutePlanning.WriteToJson(routePlanningDetail);
            _logger.LogInformation("Route planning data saved to JSON: {ResourceLink}", resourceLink);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write route planning JSON");
            return Result.Fail(ApplicationError.Internal);
        }

        missionResult.Value.SetMissionStatus(MissionStatus.Finished);
        missionResult.Value.SetMissionResourceLink(resourceLink);

        var updateResult = _missionRepository.UpdateMission(missionResult.Value);
        if (updateResult.IsFailed)
        {
            _logger.LogError("Failed to update mission entity: {ErrorMessage}", updateResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Route planning completed successfully | Mission status updated to Finished");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database commit failed after route planning");
            return Result.Fail(ApplicationError.Internal);
        }
    }

    private (bool isError, Result? value) DrawSolution(MemoryStream imageStream, IEnumerable<RouteFlowDto> routeFlows, List<RgvMap> rgvMaps, List<PathPoint> intersections, out byte[] drawnImageBytes)
    {
        drawnImageBytes = [];
        try
        {
            byte[] imageBytes = imageStream.ToArray();
            drawnImageBytes = _rgvRoutePlanning.DrawMultipleFlows(
                imageBytes,
                [.. routeFlows.Select(f => f.ArrowColor)],
                rgvMaps,
                intersections);
            _logger.LogInformation("Original route image generated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate original route image");
            return (isError: true, value: Result.Fail(ApplicationError.Internal));
        }

        return (isError: false, value: null);
    }

    private (bool isError, Result? value) ToPathPoints(IEnumerable<PathPointDto> points, out List<PathPoint> pathPoints)
    {
        pathPoints = [];
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
                _logger.LogWarning("Invalid path point: {Name} at ({Row},{Col}): {ErrorMessage}",
                    point.Name, point.Position.RowPos, point.Position.ColPos,
                    pathPointResult.Errors[0].Message);
                return (isError: true, value: Result.Fail(ApplicationError.Validation(pathPointResult.Errors[0].Message)));
            }

            pathPoints.Add(pathPointResult.Value);
        }

        return (isError: false, value: null);
    }

    private static RoutePlanningDetailDto ToRoutePlanningDto(RoutePlanningMission routePlanningMission, List<RouteSolutionDto> routeSolutions)
    {
        return new(
                    routePlanningMission.Id.ToString(),
                    routePlanningMission.Algorithm.ToString(),
                    routePlanningMission.ImageUrls,
                    routeSolutions
                );
    }
}

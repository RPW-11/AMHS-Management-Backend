using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.Mission.RoutePlanning;
using Domain.Missions;
using Domain.Missions.ValueObjects;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Application.Services.MissionService.RoutePlanningService;

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
                                   IEnumerable<RouteFlowDto> routeFlows,
                                   IEnumerable<IEnumerable<PointPositionDto>> sampleSolutions)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MissionId"] = missionId,
            ["Algorithm"] = algorithm,
            ["GridSize"]  = $"{rowDim}×{colDim}",
            ["ActualDimension"] = $"{widthLength}x{heightLength}"
        });

        _logger.LogInformation("Route planning request started | Input points: {PointCount} | Sample solutions: {SampleCount}",
            points.Count(),
            sampleSolutions.Count());

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

        // Point conversion
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
                _logger.LogWarning("Invalid path point: {Name} at ({Row},{Col}): {ErrorMessage}",
                    point.Name, point.Position.RowPos, point.Position.ColPos,
                    pathPointResult.Errors[0].Message);
                return Result.Fail(ApplicationError.Validation(pathPointResult.Errors[0].Message));
            }

            pathPoints.Add(pathPointResult.Value);
        }

        // Route flows conversion
        List<List<(int rowPos, int colPos)>> stationsOrders = [];
        foreach(var routeFlow in routeFlows)
        {
            var routeFlowPoints = routeFlow.StationsOrder
                .Select(p => (p.RowPos, p.ColPos))
                .ToList();
            stationsOrders.Add(routeFlowPoints);
        }

        // Sample solution conversion
        List<List<PathPoint>> convertedSampleSolutions = [];
        foreach (var sol in sampleSolutions)
        {
            var converted = sol.Select(p => PathPoint.Path(p.RowPos, p.ColPos)).ToList();
            convertedSampleSolutions.Add(converted);
        }

        // Maps creation
        List<RgvMap> rgvMaps = [];
        foreach (var stationOrder in stationsOrders)
        {
            // map creation
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

        // Algorithm correctness check
        var algorithmResult = RoutePlanningAlgorithm.FromString(algorithm);
        if (algorithmResult.IsFailed)
        {
            _logger.LogWarning("Unsupported or invalid algorithm: {Algorithm}: {ErrorMessage}",
                algorithm, algorithmResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation(algorithmResult.Errors[0].Message));
        }

        RoutePlanningMission routePlanningMission = RoutePlanningMission
                                                        .FromBaseClass(missionResult.Value,
                                                                    algorithmResult.Value,
                                                                    rgvMaps);
        
        // Solve every flow
        List<List<PathPoint>> postProcessedRoutesList = [];
        List<RouteSolutionDto> routeSolutions = [];
        for (int i = 0; i < routeFlows.Count(); i++)
        {
            var rgvMap = rgvMaps[i];

            var (routes, postProcessedRoutes) = _rgvRoutePlanning.Solve(rgvMap, algorithmResult.Value, convertedSampleSolutions);
            if (!routes.Any())
            {
                _logger.LogInformation("No route solution found after solving");
                return Result.Fail(ApplicationError.NotFound("No solution is found"));
            }

            _logger.LogInformation("Flow {FlowNumber} -> Route found | Original points: {Count} | Post-processed: {PostCount}",
                i + 1,routes.Count(), postProcessedRoutes.Count());

            var scores = _rgvRoutePlanning.GetRouteScore([.. routes], rgvMap);
            rgvMap.SetMapSolution([.. routes]);

            routeSolutions.Add(new(rgvMap, scores));

            postProcessedRoutesList.Add([.. postProcessedRoutes]);
        }

        string imgResultLink;
        try
        {
            byte[] imageBytes = imageStream.ToArray();
            imgResultLink = _rgvRoutePlanning.DrawMultipleFlows(imageBytes, [.. routeFlows.Select(f => f.ArrowColor)], routePlanningMission);
            _logger.LogInformation("Original route image generated: {ImageLink}", imgResultLink);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate original route image");
            return Result.Fail(ApplicationError.Internal);
        }

        routePlanningMission.AddImageUrl(imgResultLink);

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

    
        // Update the mission status to finished. Also, 
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

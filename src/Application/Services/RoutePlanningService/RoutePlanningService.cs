using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Interfaces.BackgroundJobHub;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.RoutePlanning;
using Application.Common.Utilities;
using Application.DTOs.RoutePlanning;
using Domain.Missions;
using Domain.Missions.Events;
using Domain.Missions.ValueObjects;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Services.RoutePlanningService;

public class RoutePlanningService : BaseService, IRoutePlanningService
{
    private readonly IRgvRoutePlanning _rgvRoutePlanning;
    private readonly IBackgroundJobHub _backgroundJobHub;
    private readonly IMissionRepository _missionRepository;
    private readonly ILogger<RoutePlanningService> _logger;

    public RoutePlanningService(IRgvRoutePlanning rgvRoutePlanning,
                                IBackgroundJobHub backgroundJobHub,
                                IMissionRepository missionRepository,
                                IUnitOfWork unitOfWork,
                                ILogger<RoutePlanningService> logger)
                                : base(unitOfWork)
    {
        _rgvRoutePlanning = rgvRoutePlanning;
        _backgroundJobHub = backgroundJobHub;
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

        // Maps Creation
        List<RgvMap> rgvMaps = [];
        foreach (var routeFlow in routeFlows)
        {
            var mapResult = RgvMap.Create(
                rowDim,
                colDim,
                widthLength,
                heightLength,
                pathPoints,
                [.. routeFlow.StationsOrder.Select(s => (s.RowPos, s.ColPos))],
                routeFlow.ArrowColor
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

        // Update status to Processing
        missionResult.Value.ProcessRoutePlanning(new MissionRoutePlanningStartedEvent(
            missionResult.Value.Id,
            rgvMaps,
            algorithmResult.Value,
            imageStream
        ));

        var updateResult = _missionRepository.UpdateMission(missionResult.Value);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database commit failed after route planning");
            return Result.Fail(ApplicationError.Internal);
        }

        // Solve the mission in the background
        await _backgroundJobHub.EnqueueAsync(async (sp, ct) =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var missionRepository = sp.GetRequiredService<IMissionRepository>();
            var domainDispatcher = sp.GetRequiredService<IDomainDispatcher>();
            await SolveRoute(domainDispatcher, unitOfWork, missionRepository, missionResult.Value, rgvMaps, algorithmResult.Value, imageStream);
        });

        _logger.LogInformation("Route planning is being processed | Mission status updated to Processing");

        return Result.Ok();
    }

    private async Task SolveRoute(
        IDomainDispatcher domainDispatcher,
        IUnitOfWork unitOfWork,
        IMissionRepository missionRepository,
        MissionBase mission,
        List<RgvMap> rgvMaps,
        RoutePlanningAlgorithm algorithm,
        MemoryStream imageStream)
    {
        // Solve for each flow
        List<List<PathPoint>> flowSolutions = [];
        List<List<PathPoint>> postProcessedFlowSolutions = [];
        List<RoutePlanningScoreDto> flowScores = [];

        for (int i = 0; i < rgvMaps.Count; i++)
        {
            var rgvMap = rgvMaps.ElementAt(i);

            var (routes, postProcessedRoutes) = _rgvRoutePlanning.Solve(
                rgvMap,
                flowSolutions,
                algorithm
            );

            if (!routes.Any())
            {
                _logger.LogInformation("No route solution found after solving");
                mission.SetMissionStatus(MissionStatus.Failed);
            }

            _logger.LogInformation("Flow {FlowNumber} -> Route found | Original points: {Count} | Post-processed: {PostCount}",
                i + 1, routes.Count(), postProcessedRoutes.Count());

            var scores = _rgvRoutePlanning.GetRouteScore([.. routes], rgvMap);

            flowSolutions.Add([.. routes]);
            postProcessedFlowSolutions.Add([.. postProcessedRoutes]);
            flowScores.Add(scores);
        }

        // Get intersections and draw original solution
        List<RgvMap> rgvMapsWithOriginalSolutions = [];
        for (int i = 0; i < flowSolutions.Count; i++)
        {
            var map = rgvMaps.ElementAt(i);
            rgvMapsWithOriginalSolutions.Add(new RgvMap(
                map.RowDim, map.ColDim, map.WidthLength, map.HeightLength,
                map.StationsOrder, map.MapMatrix, flowSolutions[i], map.PathColor
            ));
        }
        List<PathPoint> intersections = RouteIntersection.GetIntersectionPathPoints(flowSolutions);

        if (DrawSolution(imageStream, rgvMapsWithOriginalSolutions, intersections, out byte[] drawnImageBytes))
        {
            _logger.LogError("Error drawing the original solution");
            mission.SetMissionStatus(MissionStatus.Failed);
        }

        var originalImgUrl = _rgvRoutePlanning.WriteImage(drawnImageBytes, $"{mission.Id}_original_solution"); ;
        _logger.LogInformation("Original route image saved at: {ImageUrl}", originalImgUrl);


        // Draw post-processed solution
        List<RgvMap> rgvMapsWithPostProcessedSolutions = [];
        for (int i = 0; i < postProcessedFlowSolutions.Count; i++)
        {
            var map = rgvMaps.ElementAt(i);
            rgvMapsWithPostProcessedSolutions.Add(new RgvMap(
                map.RowDim, map.ColDim, map.WidthLength, map.HeightLength,
                map.StationsOrder, map.MapMatrix, flowSolutions[i], map.PathColor
            ));
        }
        List<PathPoint> postProcessedIntersections = RouteIntersection.GetIntersectionPathPoints(postProcessedFlowSolutions);
        if (DrawSolution(imageStream, rgvMapsWithPostProcessedSolutions, postProcessedIntersections, out byte[] postProcessedImageBytes))
        {
            _logger.LogError("Error drawing the post-processed solution");
        }
        var postProcessedImgUrl = _rgvRoutePlanning.WriteImage(postProcessedImageBytes, $"{mission.Id}_postprocessed_solution"); ;
        _logger.LogInformation("Post-processed route image saved at: {ImageUrl}", postProcessedImgUrl);

        List<RouteSolutionDto> routeSolutions = [];
        for (int i = 0; i < flowSolutions.Count; i++)
        {
            var routeSolutionDto = new RouteSolutionDto(
                rgvMapsWithOriginalSolutions[i],
                flowScores[i]
            );
            routeSolutions.Add(routeSolutionDto);
        }

        var routePlanningDetail = ToRoutePlanningDto(
            mission.Id,
            algorithm,
            [originalImgUrl, postProcessedImgUrl],
            routeSolutions);

        string resourceLink;

        try
        {
            resourceLink = _rgvRoutePlanning.WriteToJson(routePlanningDetail);
            _logger.LogInformation("Route planning data saved to JSON: {ResourceLink}", resourceLink);

            mission.Finish();
            mission.SetMissionResourceLink(resourceLink);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write route planning JSON");
            mission.SetMissionStatus(MissionStatus.Failed);
        }

        var updateResult = missionRepository.UpdateMission(mission);
        if (updateResult.IsFailed)
        {
            _logger.LogError("Failed to update mission entity: {ErrorMessage}", updateResult.Errors[0].Message);
        }

        try
        {
            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database commit failed after route planning");
        }

        await domainDispatcher.DispatchAsync(mission.DomainEvents);
        mission.ClearDomainEvents();

        _logger.LogInformation("Route planning completed successfully | Mission status updated to Finished");
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

    private bool DrawSolution(
        MemoryStream imageStream,
        List<RgvMap> rgvMaps,
        List<PathPoint> intersections, out byte[] drawnImageBytes)
    {
        drawnImageBytes = [];
        try
        {
            byte[] imageBytes = imageStream.ToArray();
            drawnImageBytes = _rgvRoutePlanning.DrawMultipleFlows(
                imageBytes,
                rgvMaps,
                intersections);
            _logger.LogInformation("Original route image generated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate original route image");
            return true;
        }

        return false;
    }

    private static RoutePlanningDetailDto ToRoutePlanningDto(
        MissionId missionId,
        RoutePlanningAlgorithm routePlanningAlgorithm,
        List<string> imageUrls,
        List<RouteSolutionDto> routeSolutions)
    {
        return new(
                    missionId.ToString(),
                    routePlanningAlgorithm.ToString(),
                    imageUrls,
                    routeSolutions
                );
    }
}

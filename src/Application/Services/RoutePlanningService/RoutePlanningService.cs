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
                                   IEnumerable<ClusterDto> clusters,
                                   IEnumerable<ClusterFlowDto> clusterFlows)
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

        var algorithmResult = RoutePlanningAlgorithm.FromString(algorithm);
        if (algorithmResult.IsFailed)
        {
            _logger.LogWarning("Unsupported or invalid algorithm: {Algorithm}: {ErrorMessage}",
                algorithm, algorithmResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation(algorithmResult.Errors[0].Message));
        }

        var pointLookup = pathPoints.ToDictionary(p => (p.RowPos, p.ColPos), p => p);

        List<Cluster> resolvedClusters = [];
        foreach (var clusterDto in clusters)
        {
            List<Station> stations = [];
            foreach (var position in clusterDto.Stations)
            {
                if (!pointLookup.TryGetValue((position.RowPos, position.ColPos), out var point))
                {
                    _logger.LogWarning("Cluster station at ({Row},{Col}) does not match any known point",
                        position.RowPos, position.ColPos);
                    return Result.Fail(ApplicationError.Validation(
                        $"Cluster station at ({position.RowPos},{position.ColPos}) does not match any known point"));
                }

                if (point is not Station station)
                {
                    _logger.LogWarning("Point at ({Row},{Col}) is not a station", position.RowPos, position.ColPos);
                    return Result.Fail(ApplicationError.Validation(
                        $"Point at ({position.RowPos},{position.ColPos}) is not a station"));
                }

                stations.Add(station);
            }

            var clusterResult = Cluster.Create(clusterDto.Name, clusterDto.ArrowColor, stations, []);
            if (clusterResult.IsFailed)
            {
                _logger.LogWarning("Failed to create cluster: {ErrorMessage}", clusterResult.Errors[0].Message);
                return Result.Fail(ApplicationError.Validation(clusterResult.Errors[0].Message));
            }

            resolvedClusters.Add(clusterResult.Value);
        }

        List<ClusterFlow> resolvedClusterFlows = [];
        foreach (var clusterFlowDto in clusterFlows)
        {
            List<Cluster> orderedClusters = [];
            foreach (var clusterIdx in clusterFlowDto.ClusterOrder)
            {
                if (clusterIdx < 0 || clusterIdx >= resolvedClusters.Count)
                {
                    _logger.LogWarning("Cluster flow references an out-of-range cluster index: {ClusterIdx}", clusterIdx);
                    return Result.Fail(ApplicationError.Validation(
                        $"A cluster flow references an out-of-range cluster index: {clusterIdx}"));
                }

                orderedClusters.Add(resolvedClusters[clusterIdx]);
            }

            var clusterFlowResult = ClusterFlow.Create(clusterFlowDto.ArrowColor, orderedClusters, []);
            if (clusterFlowResult.IsFailed)
            {
                _logger.LogWarning("Failed to create cluster flow: {ErrorMessage}", clusterFlowResult.Errors[0].Message);
                return Result.Fail(ApplicationError.Validation(clusterFlowResult.Errors[0].Message));
            }

            resolvedClusterFlows.Add(clusterFlowResult.Value);
        }

        var gridResult = Grid.Create(rowDim, colDim, widthLength, heightLength, pathPoints);
        if (gridResult.IsFailed)
        {
            _logger.LogWarning("Failed to create grid: {ErrorMessage}", gridResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation(gridResult.Errors[0].Message));
        }

        var rgvMapResult = RgvMap.Create(gridResult.Value, resolvedClusterFlows);
        if (rgvMapResult.IsFailed)
        {
            _logger.LogWarning("Failed to create RGV map: {ErrorMessage}", rgvMapResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation(rgvMapResult.Errors[0].Message));
        }

        RgvMap rgvMap = rgvMapResult.Value;

        _logger.LogDebug("RGV map created with {FlowCount} cluster flows | Grid size: {RowDim}x{ColDim}",
                rgvMap.ClusterFlows.Count, rowDim, colDim);

        // Update status to Processing
        missionResult.Value.ProcessRoutePlanning(new MissionRoutePlanningStartedEvent(
            missionResult.Value.Id,
            [],
            algorithmResult.Value,
            imageStream
        ));

        var updateResult = _missionRepository.UpdateMission(missionResult.Value);

        try
        {
            // Solve the mission in the background
            await _backgroundJobHub.EnqueueAsync(async (sp, ct) =>
            {
                var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
                var missionRepository = sp.GetRequiredService<IMissionRepository>();
                var domainDispatcher = sp.GetRequiredService<IDomainDispatcher>();
                await SolveRoute(
                    domainDispatcher, unitOfWork, missionRepository, missionResult.Value,
                    rgvMap, algorithmResult.Value, imageStream);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Full queue");
            return Result.Fail(ApplicationError.Validation("Full queue"));
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database commit failed after route planning");
            return Result.Fail(ApplicationError.Internal);
        }

        _logger.LogInformation("Route planning is being processed | Mission status updated to Processing");

        return Result.Ok();
    }

    private const int GenerationsNumber = 300;

    private async Task SolveRoute(
        IDomainDispatcher domainDispatcher,
        IUnitOfWork unitOfWork,
        IMissionRepository missionRepository,
        MissionBase mission,
        RgvMap rgvMap,
        RoutePlanningAlgorithm algorithm,
        MemoryStream imageStream)
    {
        // Solve each cluster's own route once and reuse it wherever the same cluster
        // reappears (e.g. a looping flow like C1 -> C2 -> C1).
        var clusterSolutionCache = new Dictionary<Cluster, List<PathPoint>>();

        List<(List<PathPoint> Solution, string ArrowColor)> routes = [];
        List<PathPoint> combinedSolution = [];
        List<PathPoint> combinedStationsOrder = [];
        List<ClusterFlow> solvedClusterFlows = [];

        foreach (var clusterFlow in rgvMap.ClusterFlows)
        {
            List<Cluster> solvedClusters = [];
            List<PathPoint> flowConnectorSolution = [];

            foreach (var cluster in clusterFlow.Clusters)
            {
                if (!clusterSolutionCache.TryGetValue(cluster, out var clusterSolution))
                {
                    clusterSolution = SolveClusterRoute(rgvMap.Grid, cluster, algorithm);
                    clusterSolutionCache[cluster] = clusterSolution;
                }

                var solvedCluster = Cluster.Create(cluster.Name, cluster.PathColor, cluster.Stations, clusterSolution).Value;
                solvedClusters.Add(solvedCluster);

                routes.Add((clusterSolution, cluster.PathColor));
                combinedSolution.AddRange(clusterSolution);
                combinedStationsOrder.AddRange(cluster.Stations);
            }

            // Connect each cluster to the next via the closest pair of stations (Manhattan distance).
            for (int i = 0; i < solvedClusters.Count - 1; i++)
            {
                var connectorSolution = SolveConnectorRoute(rgvMap.Grid, solvedClusters[i], solvedClusters[i + 1], algorithm);
                flowConnectorSolution.AddRange(connectorSolution);
                combinedSolution.AddRange(connectorSolution);
            }

            var solvedClusterFlow = ClusterFlow.Create(clusterFlow.PathColor, solvedClusters, flowConnectorSolution).Value;
            solvedClusterFlows.Add(solvedClusterFlow);

            routes.Add((flowConnectorSolution, clusterFlow.PathColor));
        }

        var solvedRgvMap = RgvMap.Create(rgvMap.Grid, solvedClusterFlows).Value;
        var intersections = RouteIntersection.GetIntersectionPathPoints([.. routes.Select(r => r.Solution)]);
        var score = _rgvRoutePlanning.GetRouteScore(combinedSolution, rgvMap.Grid, combinedStationsOrder);

        List<RouteSolutionDto> routeSolutions = [new(solvedRgvMap, score)];

        string resourceLink;

        try
        {
            var drawnImageBytes = _rgvRoutePlanning.DrawMultipleFlows(imageStream.ToArray(), rgvMap.Grid, routes, intersections);
            var imagePath = _rgvRoutePlanning.WriteImage(drawnImageBytes, mission.Id.ToString());

            var routePlanningDetail = ToRoutePlanningDto(
                mission.Id,
                algorithm,
                [imagePath],
                routeSolutions);

            resourceLink = _rgvRoutePlanning.WriteToJson(routePlanningDetail);
            _logger.LogInformation("Route planning data saved to JSON: {ResourceLink}", resourceLink);

            mission.Finish();
            mission.SetMissionResourceLink(resourceLink);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to solve, draw or write route planning result");
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

    private List<PathPoint> SolveClusterRoute(Grid grid, Cluster cluster, RoutePlanningAlgorithm algorithm)
    {
        _logger.LogInformation("[32m[SOLVING][0m Solving cluster {ClusterName}", cluster.Name);

        if (cluster.Stations.Count <= 1)
        {
            return [.. cluster.Stations];
        }

        var (result, _) = _rgvRoutePlanning.Solve(
            grid,
            [.. cluster.Stations.Cast<PathPoint>()],
            [],
            algorithm,
            GenerationsNumber);

        return [.. result];
    }

    private List<PathPoint> SolveConnectorRoute(Grid grid, Cluster from, Cluster to, RoutePlanningAlgorithm algorithm)
    {
        _logger.LogInformation("[32m[SOLVING][0m Solving connector for cluster {SrcClusterName} to {DstClusterName}", from.Name, to.Name);

        var (start, end) = FindNearestConnector(from.Stations, to.Stations);

        var (result, _) = _rgvRoutePlanning.Solve(
            grid,
            [start, end],
            [],
            algorithm,
            GenerationsNumber);

        return [.. result];
    }

    private static (Station Start, Station End) FindNearestConnector(IReadOnlyList<Station> from, IReadOnlyList<Station> to)
    {
        Station bestStart = from[0];
        Station bestEnd = to[0];
        int bestDistance = int.MaxValue;

        foreach (var start in from)
        {
            foreach (var end in to)
            {
                int distance = Math.Abs(start.RowPos - end.RowPos) + Math.Abs(start.ColPos - end.ColPos);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestStart = start;
                    bestEnd = end;
                }
            }
        }

        return (bestStart, bestEnd);
    }

    private (bool isError, Result? value) ToPathPoints(IEnumerable<PathPointDto> points, out List<PathPoint> pathPoints)
    {
        pathPoints = [];
        foreach (var point in points)
        {
            try
            {
                pathPoints.Add(PointFactory.Create(
                    GetPointCategoryFromString(point.Category),
                    point.Position.RowPos,
                    point.Position.ColPos,
                    point.Name,
                    point.Time
                ));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid path point: {Name} at ({Row},{Col}): {ErrorMessage}",
                    point.Name, point.Position.RowPos, point.Position.ColPos, ex.Message);
                return (isError: true, value: Result.Fail(ApplicationError.Validation(ex.Message)));
            }
        }

        return (isError: false, value: null);
    }

    private static PointCategory GetPointCategoryFromString(string category) =>
        category.ToLower() switch
        {
            "obs" => PointCategory.Obstacle,
            "st" => PointCategory.Station,
            _ => PointCategory.Path
        };

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

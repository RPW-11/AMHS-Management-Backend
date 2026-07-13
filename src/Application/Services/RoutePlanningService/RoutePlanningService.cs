using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Interfaces.BackgroundJobHub;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.RoutePlanning;
using Domain.Missions;
using Domain.Missions.ValueObjects;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Services.RoutePlanningService;

public class RoutePlanningService : BaseService, IRoutePlanningService
{
    private readonly IRouteSolver _routeSolver;
    private readonly IClusterFlowRouteSolver _clusterFlowRouteSolver;
    private readonly IRouteResultPersister _routeResultPersister;
    private readonly IBackgroundJobHub _backgroundJobHub;
    private readonly IMissionRepository _missionRepository;
    private readonly IDomainDispatcher _domainDispatcher;
    private readonly ILogger<RoutePlanningService> _logger;

    public RoutePlanningService(IRouteSolver routeSolver,
                                IClusterFlowRouteSolver clusterFlowRouteSolver,
                                IRouteResultPersister routeResultPersister,
                                IBackgroundJobHub backgroundJobHub,
                                IMissionRepository missionRepository,
                                IDomainDispatcher domainDispatcher,
                                IUnitOfWork unitOfWork,
                                ILogger<RoutePlanningService> logger)
                                : base(unitOfWork)
    {
        _routeSolver = routeSolver;
        _clusterFlowRouteSolver = clusterFlowRouteSolver;
        _routeResultPersister = routeResultPersister;
        _backgroundJobHub = backgroundJobHub;
        _missionRepository = missionRepository;
        _domainDispatcher = domainDispatcher;
        _logger = logger;
    }

    public async Task<Result> EnqueueRoutePlanning(RoutePlanningRequest request)
    {
        var (missionId, imageBytes, algorithm, rowDim, colDim, widthLength, heightLength, points, clusters, clusterFlows) = request;

        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MissionId"] = missionId,
            ["Algorithm"] = algorithm,
            ["GridSize"] = $"{rowDim}×{colDim}",
            ["ActualDimension"] = $"{widthLength}x{heightLength}"
        });

        _logger.LogInformation("Route planning request started | Input points: {PointCount}", points.Count());

        var missionIdResult = RequireValid(MissionId.FromString(missionId), "Invalid mission ID format");
        if (missionIdResult.IsFailed)
        {
            return Result.Fail(missionIdResult.Errors);
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

        var pathPointsResult = RequireValid(ToPathPoints(points), "Invalid path points");
        if (pathPointsResult.IsFailed)
        {
            return Result.Fail(pathPointsResult.Errors);
        }

        List<PathPoint> pathPoints = pathPointsResult.Value;

        var algorithmResult = RequireValid(RoutePlanningAlgorithm.FromString(algorithm), $"Unsupported or invalid algorithm '{algorithm}'");
        if (algorithmResult.IsFailed)
        {
            return Result.Fail(algorithmResult.Errors);
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

            var clusterResult = RequireValid(Cluster.Create(clusterDto.Name, clusterDto.ArrowColor, stations, []), "Failed to create cluster");
            if (clusterResult.IsFailed)
            {
                return Result.Fail(clusterResult.Errors);
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

            var clusterFlowResult = RequireValid(ClusterFlow.Create(clusterFlowDto.ArrowColor, orderedClusters, []), "Failed to create cluster flow");
            if (clusterFlowResult.IsFailed)
            {
                return Result.Fail(clusterFlowResult.Errors);
            }

            resolvedClusterFlows.Add(clusterFlowResult.Value);
        }

        var gridResult = RequireValid(Grid.Create(rowDim, colDim, widthLength, heightLength, pathPoints), "Failed to create grid");
        if (gridResult.IsFailed)
        {
            return Result.Fail(gridResult.Errors);
        }

        var rgvMapResult = RequireValid(RgvMap.Create(gridResult.Value, resolvedClusterFlows), "Failed to create RGV map");
        if (rgvMapResult.IsFailed)
        {
            return Result.Fail(rgvMapResult.Errors);
        }

        RgvMap rgvMap = rgvMapResult.Value;

        _logger.LogDebug("RGV map created with {FlowCount} cluster flows | Grid size: {RowDim}x{ColDim}",
                rgvMap.ClusterFlows.Count, rowDim, colDim);

        missionResult.Value.ProcessRoutePlanning();

        var updateResult = _missionRepository.UpdateMission(missionResult.Value);
        if (updateResult.IsFailed)
        {
            _logger.LogError("Failed to update mission in repository: {ErrorMessage}", updateResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        try
        {
            // This returns once the job is enqueued, not once it's solved — the actual
            // solve+persist runs later in its own DI scope inside the background job hub.
            await _backgroundJobHub.EnqueueAsync(async (sp, ct) =>
            {
                var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
                var missionRepository = sp.GetRequiredService<IMissionRepository>();
                var domainDispatcher = sp.GetRequiredService<IDomainDispatcher>();
                await ExecuteRoutePlanning(
                    domainDispatcher, unitOfWork, missionRepository, missionResult.Value,
                    rgvMap, algorithmResult.Value, imageBytes);
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

        await _domainDispatcher.DispatchAsync(missionResult.Value.DomainEvents);
        missionResult.Value.ClearDomainEvents();

        _logger.LogInformation("Route planning is being processed | Mission status updated to Processing");

        return Result.Ok();
    }

    private async Task ExecuteRoutePlanning(
        IDomainDispatcher domainDispatcher,
        IUnitOfWork unitOfWork,
        IMissionRepository missionRepository,
        MissionBase mission,
        RgvMap rgvMap,
        RoutePlanningAlgorithm algorithm,
        byte[] imageBytes)
    {
        try
        {
            // Solve each cluster's own route once and reuse it wherever the same cluster
            // reappears (e.g. a looping flow like C1 -> C2 -> C1).
            var clusterSolutionCache = new Dictionary<Cluster, List<PathPoint>>();

            // Every already-solved segment (cluster loops + connectors) is fed into subsequent
            // solves so the GA can penalize new routes that traverse an existing one in reverse.
            List<List<PathPoint>> solvedRouteSegments = [];

            List<(List<PathPoint> Solution, string ArrowColor)> routes = [];
            List<PathPoint> combinedSolution = [];
            List<PathPoint> combinedStationsOrder = [];
            List<ClusterFlow> solvedClusterFlows = [];

            foreach (var clusterFlow in rgvMap.ClusterFlows)
            {
                List<Cluster> solvedClusters = [];
                List<List<PathPoint>> connectorSolutions = [];

                foreach (var cluster in clusterFlow.Clusters)
                {
                    if (!clusterSolutionCache.TryGetValue(cluster, out var clusterSolution))
                    {
                        clusterSolution = _clusterFlowRouteSolver.SolveClusterRoute(rgvMap.Grid, cluster, algorithm, solvedRouteSegments);
                        clusterSolutionCache[cluster] = clusterSolution;
                        solvedRouteSegments.Add(clusterSolution);
                    }

                    var solvedCluster = Cluster.Create(cluster.Name, cluster.PathColor, cluster.Stations, clusterSolution).Value;
                    solvedClusters.Add(solvedCluster);

                    routes.Add((clusterSolution, cluster.PathColor));
                    combinedSolution.AddRange(clusterSolution);
                    combinedStationsOrder.AddRange(cluster.Stations);
                }

                for (int i = 0; i < solvedClusters.Count - 1; i++)
                {
                    var connectorSolution = _clusterFlowRouteSolver.SolveConnectorRoute(rgvMap.Grid, solvedClusters[i], solvedClusters[i + 1], algorithm, solvedRouteSegments);
                    solvedRouteSegments.Add(connectorSolution);
                    connectorSolutions.Add(connectorSolution);
                    combinedSolution.AddRange(connectorSolution);

                    // Draw each connector as its own polyline so unrelated connectors in a
                    // multi-hop flow don't get joined by an unsolved straight line.
                    routes.Add((connectorSolution, clusterFlow.PathColor));
                }

                var solvedClusterFlow = ClusterFlow.Create(clusterFlow.PathColor, solvedClusters, connectorSolutions).Value;
                solvedClusterFlows.Add(solvedClusterFlow);
            }

            var solvedRgvMap = RgvMap.Create(rgvMap.Grid, solvedClusterFlows).Value;
            var score = _routeSolver.GetRouteScore(combinedSolution, rgvMap.Grid, combinedStationsOrder);

            _routeResultPersister.Persist(mission, rgvMap.Grid, algorithm, imageBytes, routes, ToRgvMapDetailDto(solvedRgvMap.Grid), ToClusterFlowSolutionDtos(solvedClusterFlows), score);

            _logger.LogInformation("Route planning completed successfully | Mission status updated to Finished");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route planning failed for mission {MissionId}", mission.Id);
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
    }

    private static RgvMapDetailDto ToRgvMapDetailDto(Grid grid)
    {
        List<List<PathPointDto>> mapMatrix = [];
        for (int row = 0; row < grid.RowDim; row++)
        {
            List<PathPointDto> rowPoints = [];
            for (int col = 0; col < grid.ColDim; col++)
            {
                rowPoints.Add(ToPathPointDto(grid.MapMatrix[row, col]));
            }
            mapMatrix.Add(rowPoints);
        }

        return new RgvMapDetailDto(grid.RowDim, grid.ColDim, grid.WidthLength, grid.HeightLength, mapMatrix);
    }

    private static List<ClusterFlowSolutionDto> ToClusterFlowSolutionDtos(IEnumerable<ClusterFlow> clusterFlows) =>
        [.. clusterFlows.Select(clusterFlow => new ClusterFlowSolutionDto(
            clusterFlow.PathColor,
            [.. clusterFlow.Clusters.Select(cluster => new ClusterSolutionDto(
                cluster.Name,
                cluster.PathColor,
                [.. cluster.Solution.Select(ToPathPointDto)]
            ))],
            [.. clusterFlow.ConnectorSolutions.Select(connectorSolution => new List<PathPointDto>([.. connectorSolution.Select(ToPathPointDto)]))]
        ))];

    private static PathPointDto ToPathPointDto(PathPoint point) =>
        point switch
        {
            Station station => new PathPointDto(station.Name, "st", new(station.RowPos, station.ColPos), station.ProcessingTime),
            Obstacle => new PathPointDto("", "obs", new(point.RowPos, point.ColPos), 0),
            _ => new PathPointDto("", "path", new(point.RowPos, point.ColPos), 0)
        };

    private static Result<List<PathPoint>> ToPathPoints(IEnumerable<PathPointDto> points)
    {
        List<PathPoint> pathPoints = [];
        foreach (var point in points)
        {
            var pointResult = PointFactory.Create(
                GetPointCategoryFromString(point.Category),
                point.Position.RowPos,
                point.Position.ColPos,
                point.Name,
                point.Time
            );

            if (pointResult.IsFailed)
            {
                return Result.Fail(pointResult.Errors);
            }

            pathPoints.Add(pointResult.Value);
        }

        return Result.Ok(pathPoints);
    }

    private Result<T> RequireValid<T>(Result<T> result, string context)
    {
        if (result.IsFailed)
        {
            _logger.LogWarning("{Context}: {ErrorMessage}", context, result.Errors[0].Message);
            return Result.Fail<T>(ApplicationError.Validation(result.Errors[0].Message));
        }

        return result;
    }

    private static PointCategory GetPointCategoryFromString(string category) =>
        category.ToLower() switch
        {
            "obs" => PointCategory.Obstacle,
            "st" => PointCategory.Station,
            _ => PointCategory.Path
        };

}

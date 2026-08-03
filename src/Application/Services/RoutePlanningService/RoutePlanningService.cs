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
    private readonly IPathfindingStrategyProvider _strategyProvider;
    private readonly IRouteScorer _routeScorer;
    private readonly IClusterFlowRouteSolver _clusterFlowRouteSolver;
    private readonly IRouteResultPersister _routeResultPersister;
    private readonly ISourceImageValidator _sourceImageValidator;
    private readonly IBackgroundJobHub _backgroundJobHub;
    private readonly IMissionRepository _missionRepository;
    private readonly IDomainDispatcher _domainDispatcher;
    private readonly ILogger<RoutePlanningService> _logger;

    public RoutePlanningService(IPathfindingStrategyProvider strategyProvider,
                                IRouteScorer routeScorer,
                                IClusterFlowRouteSolver clusterFlowRouteSolver,
                                IRouteResultPersister routeResultPersister,
                                ISourceImageValidator sourceImageValidator,
                                IBackgroundJobHub backgroundJobHub,
                                IMissionRepository missionRepository,
                                IDomainDispatcher domainDispatcher,
                                IUnitOfWork unitOfWork,
                                ILogger<RoutePlanningService> logger)
                                : base(unitOfWork)
    {
        _strategyProvider = strategyProvider;
        _routeScorer = routeScorer;
        _clusterFlowRouteSolver = clusterFlowRouteSolver;
        _routeResultPersister = routeResultPersister;
        _sourceImageValidator = sourceImageValidator;
        _backgroundJobHub = backgroundJobHub;
        _missionRepository = missionRepository;
        _domainDispatcher = domainDispatcher;
        _logger = logger;
    }

    public async Task<Result> EnqueueRoutePlanning(RoutePlanningRequest request)
    {
        var (missionId, imageBytes, imageContentType, algorithm, rowDim, colDim, widthLength, heightLength, points, clusters, clusterFlows) = request;

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
        if (missionResult.IsFailed)
        {
            _logger.LogError("Failed to load mission from repository: {ErrorMessage}", missionResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        if (missionResult.Value is null)
        {
            _logger.LogWarning("Mission not found");
            return Result.Fail(ApplicationError.NotFound("Mission is not found"));
        }

        if (missionResult.Value.Category != MissionCategory.RoutePlanning)
        {
            _logger.LogWarning("Mission category mismatch - expected RoutePlanning, got {Category}",
                missionResult.Value.Category);
            return Result.Fail(ApplicationError.Validation("The selected mission is not a route-planning mission"));
        }

        if (missionResult.Value.Status == MissionStatus.Processing)
        {
            _logger.LogWarning("Route planning is already in progress for this mission");
            return Result.Fail(ApplicationError.Duplicated("Route planning is already in progress for this mission"));
        }

        _logger.LogDebug("Mission validated | Category: {Category} | Name: {Name}",
            missionResult.Value.Category, missionResult.Value.Name ?? "(no name)");

        var imageResult = _sourceImageValidator.Validate(imageBytes, imageContentType);
        if (imageResult.IsFailed)
        {
            _logger.LogWarning("Rejected source layout image: {ErrorMessage}", imageResult.Errors[0].Message);
            return Result.Fail(imageResult.Errors);
        }

        var algorithmResult = RequireValid(RoutePlanningAlgorithm.FromString(algorithm), $"Unsupported or invalid algorithm '{algorithm}'");
        if (algorithmResult.IsFailed)
        {
            return Result.Fail(algorithmResult.Errors);
        }

        var strategyResult = _strategyProvider.GetStrategy(algorithmResult.Value);
        if (strategyResult.IsFailed)
        {
            _logger.LogWarning("Algorithm '{Algorithm}' has no registered strategy", algorithm);
            return Result.Fail(strategyResult.Errors);
        }

        var rgvMapResult = new RgvMapBuilder()
            .WithGrid(rowDim, colDim, widthLength, heightLength)
            .WithPoints(points)
            .WithClusters(clusters)
            .WithClusterFlows(clusterFlows)
            .Build();

        if (rgvMapResult.IsFailed)
        {
            _logger.LogWarning("Failed to build the RGV map: {ErrorMessage}", rgvMapResult.Errors[0].Message);
            return Result.Fail(rgvMapResult.Errors);
        }

        RgvMap rgvMap = rgvMapResult.Value;

        _logger.LogDebug("RGV map created with {FlowCount} cluster flows | Grid size: {RowDim}x{ColDim}",
                rgvMap.ClusterFlows.Count, rowDim, colDim);

        MissionBase mission = missionResult.Value;
        MissionId parsedMissionId = missionIdResult.Value;

        mission.ProcessRoutePlanning();

        var updateResult = _missionRepository.UpdateMission(mission);
        if (updateResult.IsFailed)
        {
            _logger.LogError("Failed to update mission in repository: {ErrorMessage}", updateResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database commit failed before enqueueing route planning");
            return Result.Fail(ApplicationError.Internal);
        }

        await _domainDispatcher.DispatchAsync(mission.DomainEvents);
        mission.ClearDomainEvents();

        bool enqueued = _backgroundJobHub.TryEnqueue(async (sp, ct) =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var missionRepository = sp.GetRequiredService<IMissionRepository>();
            var domainDispatcher = sp.GetRequiredService<IDomainDispatcher>();

            var jobMissionResult = await missionRepository.GetMissionByIdAsync(parsedMissionId);
            if (jobMissionResult.IsFailed)
            {
                // Nothing to flip to Failed: the mission could not be loaded, so it stays
                // Processing until the reconciliation sweep picks it up.
                _logger.LogError("Failed to reload mission {MissionId} for its route planning job: {ErrorMessage}",
                    parsedMissionId, jobMissionResult.Errors[0].Message);
                return;
            }

            if (jobMissionResult.Value is null)
            {
                _logger.LogError("Mission {MissionId} no longer exists, abandoning its route planning job", parsedMissionId);
                return;
            }

            await ExecuteRoutePlanning(
                domainDispatcher, unitOfWork, missionRepository, jobMissionResult.Value,
                rgvMap, strategyResult.Value, imageBytes);
        }, out _);

        if (!enqueued)
        {
            _logger.LogWarning("Route planning queue is full, mission {MissionId} was not enqueued", parsedMissionId);
            await RevertProcessingStatus(mission);
            return Result.Fail(ApplicationError.Validation("The route planning queue is full, please try again later"));
        }

        _logger.LogInformation("Route planning is being processed | Mission status updated to Processing");

        return Result.Ok();
    }

    private async Task RevertProcessingStatus(MissionBase mission)
    {
        mission.SetMissionStatus(MissionStatus.Failed);

        var revertResult = _missionRepository.UpdateMission(mission);
        if (revertResult.IsFailed)
        {
            _logger.LogError("Failed to revert mission status after a rejected enqueue: {ErrorMessage}", revertResult.Errors[0].Message);
            return;
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit the reverted mission status after a rejected enqueue");
        }
    }

    private async Task ExecuteRoutePlanning(
        IDomainDispatcher domainDispatcher,
        IUnitOfWork unitOfWork,
        IMissionRepository missionRepository,
        MissionBase mission,
        RgvMap rgvMap,
        IPathfindingStrategy strategy,
        byte[] imageBytes)
    {
        try
        {
            var clusterSolutionCache = new Dictionary<Cluster, List<PathPoint>>();

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
                        clusterSolution = _clusterFlowRouteSolver.SolveClusterRoute(rgvMap.Grid, cluster, strategy, solvedRouteSegments);
                        clusterSolutionCache[cluster] = clusterSolution;
                        solvedRouteSegments.Add(clusterSolution);
                    }

                    var solvedCluster = RequireSolved(
                        Cluster.Create(cluster.Name, cluster.PathColor, cluster.Stations, clusterSolution),
                        $"Failed to rebuild solved cluster '{cluster.Name}'");
                    solvedClusters.Add(solvedCluster);

                    routes.Add((clusterSolution, cluster.PathColor));
                    combinedSolution.AddRange(clusterSolution);
                    combinedStationsOrder.AddRange(cluster.Stations);
                }

                for (int i = 0; i < solvedClusters.Count - 1; i++)
                {
                    var connectorSolution = _clusterFlowRouteSolver.SolveConnectorRoute(rgvMap.Grid, solvedClusters[i], solvedClusters[i + 1], strategy, solvedRouteSegments);
                    solvedRouteSegments.Add(connectorSolution);
                    connectorSolutions.Add(connectorSolution);
                    combinedSolution.AddRange(connectorSolution);

                    routes.Add((connectorSolution, clusterFlow.PathColor));
                }

                var solvedClusterFlow = RequireSolved(
                    ClusterFlow.Create(clusterFlow.PathColor, solvedClusters, connectorSolutions),
                    "Failed to rebuild solved cluster flow");
                solvedClusterFlows.Add(solvedClusterFlow);
            }

            var solvedRgvMap = RequireSolved(
                RgvMap.Create(rgvMap.Grid, solvedClusterFlows),
                "Failed to rebuild solved RGV map");
            var score = _routeScorer.GetRouteScore(combinedSolution, rgvMap.Grid, combinedStationsOrder, RouteSolvePurpose.Connector);

            _routeResultPersister.Persist(
                mission, rgvMap.Grid, strategy.Algorithm, imageBytes, routes,
                ToRgvMapDetailDto(solvedRgvMap.Grid),
                ToClusterDefinitionDtos(rgvMap),
                ToClusterFlowDefinitionDtos(rgvMap),
                ToClusterFlowSolutionDtos(solvedClusterFlows),
                score);

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
                rowPoints.Add(ToPathPointDto(grid[row, col]));
            }
            mapMatrix.Add(rowPoints);
        }

        return new RgvMapDetailDto(grid.RowDim, grid.ColDim, grid.WidthLength, grid.HeightLength, mapMatrix);
    }

    private static List<ClusterDefinitionDto> ToClusterDefinitionDtos(RgvMap rgvMap) =>
        [.. rgvMap.ClusterFlows
            .SelectMany(clusterFlow => clusterFlow.Clusters)
            .DistinctBy(cluster => cluster.Name)
            .Select(cluster => new ClusterDefinitionDto(
                cluster.Name,
                cluster.PathColor,
                [.. cluster.Stations.Select(ToPathPointDto)]
            ))];

    private static List<ClusterFlowDefinitionDto> ToClusterFlowDefinitionDtos(RgvMap rgvMap) =>
        [.. rgvMap.ClusterFlows.Select(clusterFlow => new ClusterFlowDefinitionDto(
            clusterFlow.PathColor,
            [.. clusterFlow.Clusters.Select(cluster => cluster.Name)]
        ))];

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

    private static T RequireSolved<T>(Result<T> result, string context)
    {
        if (result.IsFailed)
        {
            throw new InvalidOperationException($"{context}: {result.Errors[0].Message}");
        }

        return result.Value;
    }

    private Result<T> RequireValid<T>(Result<T> result, string context)
    {
        if (result.IsFailed)
        {
            var error = result.Errors[0];
            _logger.LogWarning("{Context}: {ErrorMessage}", context, error.Message);

            string detail = error.Metadata.TryGetValue("detail", out var value) ? value?.ToString() ?? "" : "";

            return Result.Fail<T>(new ApplicationError(error.Message, "Validation", detail));
        }

        return result;
    }
}

using Application.Common.Interfaces;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.RoutePlanning;
using Domain.Missions;
using Domain.Missions.ValueObjects;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Application.Services.RoutePlanningService;

public class RoutePlanningJobHandler : IRoutePlanningJobHandler
{
    private readonly IPathfindingStrategyProvider _strategyProvider;
    private readonly IClusterFlowRouteSolver _clusterFlowRouteSolver;
    private readonly IRouteResultPersister _routeResultPersister;
    private readonly IRouteScorer _routeScorer;
    private readonly IMissionRepository _missionRepository;
    private readonly IDomainDispatcher _domainDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RoutePlanningJobHandler> _logger;

    public RoutePlanningJobHandler(IPathfindingStrategyProvider strategyProvider,
                                   IClusterFlowRouteSolver clusterFlowRouteSolver,
                                   IRouteResultPersister routeResultPersister,
                                   IRouteScorer routeScorer,
                                   IMissionRepository missionRepository,
                                   IDomainDispatcher domainDispatcher,
                                   IUnitOfWork unitOfWork,
                                   ILogger<RoutePlanningJobHandler> logger)
    {
        _strategyProvider = strategyProvider;
        _clusterFlowRouteSolver = clusterFlowRouteSolver;
        _routeResultPersister = routeResultPersister;
        _routeScorer = routeScorer;
        _missionRepository = missionRepository;
        _domainDispatcher = domainDispatcher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(RoutePlanningJob job, CancellationToken cancellationToken = default)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MissionId"] = job.MissionId.ToString(),
            ["Algorithm"] = job.Algorithm.ToString()
        });

        var missionResult = await _missionRepository.GetMissionByIdAsync(job.MissionId);
        if (missionResult.IsFailed)
        {
            // Nothing to flip to Failed: the mission could not be loaded, so it stays
            // Processing until the reconciliation sweep picks it up.
            _logger.LogError("Failed to reload mission for its route planning job: {ErrorMessage}",
                missionResult.Errors[0].Message);
            return;
        }

        if (missionResult.Value is null)
        {
            _logger.LogError("Mission no longer exists, abandoning its route planning job");
            return;
        }

        MissionBase mission = missionResult.Value;

        try
        {
            Solve(mission, job);
            _logger.LogInformation("Route planning completed successfully | Mission status updated to Finished");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route planning failed");
            mission.SetMissionStatus(MissionStatus.Failed);
        }

        var updateResult = _missionRepository.UpdateMission(mission);
        if (updateResult.IsFailed)
        {
            _logger.LogError("Failed to update mission entity: {ErrorMessage}", updateResult.Errors[0].Message);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database commit failed after route planning");
        }

        await _domainDispatcher.DispatchAsync(mission.DomainEvents);
        mission.ClearDomainEvents();
    }

    private void Solve(MissionBase mission, RoutePlanningJob job)
    {
        RgvMap rgvMap = job.RgvMap;

        var strategyResult = _strategyProvider.GetStrategy(job.Algorithm);
        if (strategyResult.IsFailed)
        {
            throw new InvalidOperationException(
                $"No registered pathfinding strategy for '{job.Algorithm}': {strategyResult.Errors[0].Message}");
        }

        IPathfindingStrategy strategy = strategyResult.Value;

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
            mission, rgvMap.Grid, strategy.Algorithm, job.ImageBytes, routes,
            RoutePlanningDtoMapper.ToRgvMapDetailDto(solvedRgvMap.Grid),
            RoutePlanningDtoMapper.ToClusterDefinitionDtos(rgvMap),
            RoutePlanningDtoMapper.ToClusterFlowDefinitionDtos(rgvMap),
            RoutePlanningDtoMapper.ToClusterFlowSolutionDtos(solvedClusterFlows),
            score);
    }

    private static T RequireSolved<T>(Result<T> result, string context)
    {
        if (result.IsFailed)
        {
            throw new InvalidOperationException($"{context}: {result.Errors[0].Message}");
        }

        return result.Value;
    }
}

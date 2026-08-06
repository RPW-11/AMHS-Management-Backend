using Application.Common.Interfaces;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.RoutePlanning;
using Domain.Missions;
using Domain.Missions.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Services.RoutePlanningService;

public class RoutePlanningJobHandler : IRoutePlanningJobHandler
{
    /// <summary>
    /// How many complete routings to solve. Fewer are stored when the layout admits fewer.
    /// </summary>
    private const int TargetSolutionCount = 5;

    private readonly IPathfindingStrategyProvider _strategyProvider;
    private readonly IClusterFlowRouteSolver _clusterFlowRouteSolver;
    private readonly IRouteSolutionComposer _routeSolutionComposer;
    private readonly IRouteResultPersister _routeResultPersister;
    private readonly IMissionRepository _missionRepository;
    private readonly IDomainDispatcher _domainDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RoutePlanningJobHandler> _logger;

    public RoutePlanningJobHandler(IPathfindingStrategyProvider strategyProvider,
                                   IClusterFlowRouteSolver clusterFlowRouteSolver,
                                   IRouteSolutionComposer routeSolutionComposer,
                                   IRouteResultPersister routeResultPersister,
                                   IMissionRepository missionRepository,
                                   IDomainDispatcher domainDispatcher,
                                   IUnitOfWork unitOfWork,
                                   ILogger<RoutePlanningJobHandler> logger)
    {
        _strategyProvider = strategyProvider;
        _clusterFlowRouteSolver = clusterFlowRouteSolver;
        _routeSolutionComposer = routeSolutionComposer;
        _routeResultPersister = routeResultPersister;
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
            await SolveAsync(mission, job, cancellationToken);
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

    private async Task SolveAsync(MissionBase mission, RoutePlanningJob job, CancellationToken cancellationToken)
    {
        RgvMap rgvMap = job.RgvMap;

        var strategyResult = _strategyProvider.GetStrategy(job.Algorithm);
        if (strategyResult.IsFailed)
        {
            throw new InvalidOperationException(
                $"No registered pathfinding strategy for '{job.Algorithm}': {strategyResult.Errors[0].Message}");
        }

        IPathfindingStrategy strategy = strategyResult.Value;

        var clusterLoops = SolveClusterLoops(rgvMap, strategy);

        var solutions = _routeSolutionComposer.Compose(rgvMap, strategy, clusterLoops, TargetSolutionCount);

        ComposedSolution bestSolution = solutions[0];

        // Only the connector solutions differ between the composed routings, and RgvMap validates
        // clusters and stations, so validating one routing validates them all.
        var solvedRgvMap = SolvedResult.Require(
            RgvMap.Create(rgvMap.Grid, bestSolution.ClusterFlows),
            "Failed to rebuild solved RGV map");

        await _routeResultPersister.PersistAsync(
            mission, rgvMap.Grid, strategy.Algorithm, job.ImageBytes, bestSolution.Routes,
            RoutePlanningDtoMapper.ToRgvMapDetailDto(solvedRgvMap.Grid),
            RoutePlanningDtoMapper.ToClusterDefinitionDtos(rgvMap),
            RoutePlanningDtoMapper.ToClusterFlowDefinitionDtos(rgvMap),
            RoutePlanningDtoMapper.ToClusterFlowSolutionDtos(bestSolution.ClusterFlows),
            bestSolution.Score, cancellationToken);
    }

    /// <summary>
    /// Solves every unique cluster's own visiting order, before any connector is solved and with
    /// only the other loops as conflict context.
    /// </summary>
    /// <remarks>
    /// A cluster is one piece of physical track, shared by every flow that visits it, so its loop
    /// must not depend on which flow happened to reach it first or on any connector routed around
    /// it. Solving the loops as their own phase is what lets the connector solutions branch later
    /// while every branch keeps identical cluster track.
    /// </remarks>
    private ClusterLoopSolutions SolveClusterLoops(RgvMap rgvMap, IPathfindingStrategy strategy)
    {
        Dictionary<Cluster, List<PathPoint>> solutionsByCluster = [];
        List<List<PathPoint>> segments = [];
        List<IReadOnlyList<Station>> stations = [];

        foreach (var cluster in rgvMap.ClusterFlows.SelectMany(clusterFlow => clusterFlow.Clusters))
        {
            // A cluster revisited later in a looping flow, or shared between flows, is the same
            // track: solve it once and reuse it.
            if (solutionsByCluster.ContainsKey(cluster))
            {
                continue;
            }

            var solution = _clusterFlowRouteSolver.SolveClusterRoute(rgvMap.Grid, cluster, strategy, segments);

            solutionsByCluster[cluster] = solution;
            segments.Add(solution);
            stations.Add(cluster.Stations);
        }

        _logger.LogDebug("Solved {ClusterCount} unique cluster loops", solutionsByCluster.Count);

        return new ClusterLoopSolutions(solutionsByCluster, segments, stations);
    }
}

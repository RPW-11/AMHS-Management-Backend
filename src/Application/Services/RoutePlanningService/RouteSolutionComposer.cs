using Application.Common.Interfaces.RoutePlanning;
using Domain.Missions.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Services.RoutePlanningService;

/// <summary>
/// Produces several complete routings of a map by solving the connectors more than once.
/// </summary>
/// <remarks>
/// Every routing is solved as its own lane. A lane solves each connector in turn against the
/// connectors already chosen <em>in that same lane</em>, so a stored routing only ever contains
/// segments that were scored against the segments shipping alongside them.
/// <para>
/// Combining segments across lanes afterwards would not do: a connector is scored for conflicts
/// against the routes it was solved with, so pairing it with a different set of neighbours than
/// the one it avoided would give a routing whose conflicts nothing ever evaluated.
/// </para>
/// <para>
/// The lanes are pushed apart once, at the first connector, which is solved a single time for
/// several distinct answers. Everything after that follows from the context each lane accumulates.
/// This costs one solve for the first connector and one per lane for each of the rest - linear in
/// the lane count, not exponential, because a lane never branches.
/// </para>
/// </remarks>
public class RouteSolutionComposer(
    IClusterFlowRouteSolver clusterFlowRouteSolver,
    IRouteScorer routeScorer,
    ILogger<RouteSolutionComposer> logger) : IRouteSolutionComposer
{
    private readonly IClusterFlowRouteSolver _clusterFlowRouteSolver = clusterFlowRouteSolver;
    private readonly IRouteScorer _routeScorer = routeScorer;
    private readonly ILogger<RouteSolutionComposer> _logger = logger;

    public IReadOnlyList<ComposedSolution> Compose(
        RgvMap rgvMap,
        IPathfindingStrategy strategy,
        ClusterLoopSolutions clusterLoops,
        int desiredSolutions)
    {
        var solvedFlowClusters = BuildSolvedClusters(rgvMap, clusterLoops);
        var slots = BuildConnectorSlots(rgvMap, solvedFlowClusters);

        if (slots.Count == 0)
        {
            // Nothing to vary: every flow is a single cluster, so the loops already are the routing.
            _logger.LogInformation("Map has no connectors, composing the single possible routing");

            return [Assemble(rgvMap, clusterLoops, solvedFlowClusters, [])];
        }

        var lanes = SolveLanes(rgvMap.Grid, strategy, clusterLoops, slots, Math.Max(1, desiredSolutions));

        LogLaneDivergence(lanes);

        var solutions = lanes
            .Select(lane => Assemble(rgvMap, clusterLoops, solvedFlowClusters, lane))
            .OrderByDescending(solution => solution.Score.Optimality)
            .ToList();

        _logger.LogInformation("Composed {SolutionCount} routings of {Requested} requested | Optimality {Optimality}",
            solutions.Count, desiredSolutions, string.Join(", ", solutions.Select(solution => solution.Score.Optimality.ToString("F2"))));

        return solutions;
    }

    /// <summary>
    /// Solves one connector chain per lane. Lane 0 runs cold and every later lane warm starts from
    /// what lane 0 found for the same connector, which differs only in the routes it had to avoid.
    /// </summary>
    private List<List<List<PathPoint>>> SolveLanes(
        Grid grid,
        IPathfindingStrategy strategy,
        ClusterLoopSolutions clusterLoops,
        IReadOnlyList<ConnectorSlot> slots,
        int desiredSolutions)
    {
        ConnectorSlot rootSlot = slots[0];

        var rootVariants = _clusterFlowRouteSolver.SolveConnectorRoutes(
            grid, rootSlot.From, rootSlot.To, strategy, [.. clusterLoops.Segments], desiredSolutions);

        _logger.LogInformation("Root connector yielded {VariantCount} distinct routes, seeding that many lanes",
            rootVariants.Count);

        List<List<List<PathPoint>>> lanes = [];

        foreach (var rootVariant in rootVariants)
        {
            bool isFirstLane = lanes.Count == 0;

            List<List<PathPoint>> context = [.. clusterLoops.Segments, rootVariant];
            List<List<PathPoint>> laneSolutions = [rootVariant];

            for (int slotIndex = 1; slotIndex < slots.Count; slotIndex++)
            {
                ConnectorSlot slot = slots[slotIndex];

                IReadOnlyList<List<PathPoint>>? seedPaths = isFirstLane ? null : [lanes[0][slotIndex]];

                List<PathPoint> solution = _clusterFlowRouteSolver.SolveConnectorRoutes(
                    grid, slot.From, slot.To, strategy, context, desiredSolutions: 1, seedPaths)[0];

                laneSolutions.Add(solution);

                // The next connector in this lane avoids what this lane just committed to.
                context.Add(solution);
            }

            lanes.Add(laneSolutions);
        }

        return lanes;
    }

    /// <summary>
    /// Turns one lane's connector solutions into a whole routing: the shared cluster loops, this
    /// lane's connectors, and a score over the two together.
    /// </summary>
    private ComposedSolution Assemble(
        RgvMap rgvMap,
        ClusterLoopSolutions clusterLoops,
        IReadOnlyList<List<Cluster>> solvedFlowClusters,
        IReadOnlyList<List<PathPoint>> laneSolutions)
    {
        List<ClusterFlow> clusterFlows = [];
        List<(List<PathPoint> Solution, string ArrowColor)> routes = [];

        // Loop points come first and appear once per unique cluster, matching the order they were
        // solved in - a cluster revisited in a looping flow is one piece of track, counted once.
        List<PathPoint> combinedSolution = [.. clusterLoops.Segments.SelectMany(segment => segment)];

        int slotIndex = 0;

        for (int flowIndex = 0; flowIndex < rgvMap.ClusterFlows.Count; flowIndex++)
        {
            ClusterFlow clusterFlow = rgvMap.ClusterFlows[flowIndex];
            List<Cluster> solvedClusters = solvedFlowClusters[flowIndex];

            foreach (var cluster in clusterFlow.Clusters)
            {
                routes.Add((clusterLoops.SolutionsByCluster[cluster], cluster.PathColor));
            }

            List<List<PathPoint>> connectorSolutions = [];

            for (int i = 0; i < solvedClusters.Count - 1; i++)
            {
                List<PathPoint> connectorSolution = laneSolutions[slotIndex++];

                connectorSolutions.Add(connectorSolution);
                combinedSolution.AddRange(connectorSolution);

                routes.Add((connectorSolution, clusterFlow.PathColor));
            }

            clusterFlows.Add(SolvedResult.Require(
                ClusterFlow.Create(clusterFlow.PathColor, solvedClusters, connectorSolutions),
                "Failed to rebuild solved cluster flow"));
        }

        var score = _routeScorer.GetRouteScore(combinedSolution, rgvMap.Grid, clusterLoops.Stations, RouteSolvePurpose.Connector);

        return new ComposedSolution(clusterFlows, routes, score);
    }

    /// <summary>
    /// Rebuilds each flow's clusters carrying their solved loops. Lane independent: the loops are
    /// shared, so this is done once and reused by every routing.
    /// </summary>
    private static List<List<Cluster>> BuildSolvedClusters(RgvMap rgvMap, ClusterLoopSolutions clusterLoops) =>
        [.. rgvMap.ClusterFlows.Select(clusterFlow =>
            clusterFlow.Clusters
                .Select(cluster => SolvedResult.Require(
                    Cluster.Create(cluster.Name, cluster.PathColor, cluster.Stations, clusterLoops.SolutionsByCluster[cluster]),
                    $"Failed to rebuild solved cluster '{cluster.Name}'"))
                .ToList())];

    /// <summary>
    /// Every connector on the map, flattened across flows into one order. A lane solves them in
    /// this order, so a lane's solutions can be read back into the flows they came from by
    /// walking the same order again.
    /// </summary>
    private static List<ConnectorSlot> BuildConnectorSlots(RgvMap rgvMap, IReadOnlyList<List<Cluster>> solvedFlowClusters)
    {
        List<ConnectorSlot> slots = [];

        for (int flowIndex = 0; flowIndex < rgvMap.ClusterFlows.Count; flowIndex++)
        {
            List<Cluster> solvedClusters = solvedFlowClusters[flowIndex];

            for (int i = 0; i < solvedClusters.Count - 1; i++)
            {
                slots.Add(new ConnectorSlot(solvedClusters[i], solvedClusters[i + 1]));
            }
        }

        return slots;
    }

    /// <summary>
    /// Reports how far each lane actually moved from lane 0. Lanes that all report zero mean the
    /// search reconverged and the extra solves bought nothing.
    /// </summary>
    private void LogLaneDivergence(IReadOnlyList<List<List<PathPoint>>> lanes)
    {
        if (!_logger.IsEnabled(LogLevel.Debug) || lanes.Count < 2)
        {
            return;
        }

        for (int laneIndex = 1; laneIndex < lanes.Count; laneIndex++)
        {
            int differing = 0;

            for (int slotIndex = 0; slotIndex < lanes[laneIndex].Count; slotIndex++)
            {
                if (!IsSamePath(lanes[laneIndex][slotIndex], lanes[0][slotIndex]))
                {
                    differing++;
                }
            }

            _logger.LogDebug("Lane {LaneIndex} differs from lane 0 in {DifferingCount} of {ConnectorCount} connectors",
                laneIndex, differing, lanes[laneIndex].Count);
        }
    }

    private static bool IsSamePath(List<PathPoint> left, List<PathPoint> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (int i = 0; i < left.Count; i++)
        {
            if (left[i].RowPos != right[i].RowPos || left[i].ColPos != right[i].ColPos)
            {
                return false;
            }
        }

        return true;
    }

    private sealed record ConnectorSlot(Cluster From, Cluster To);
}

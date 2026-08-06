using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.RoutePlanning;
using Domain.Missions.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Services.RoutePlanningService;

public class ClusterFlowRouteSolver(IRouteScorer routeScorer, ILogger<ClusterFlowRouteSolver> logger) : IClusterFlowRouteSolver
{
    private const int ClusterPermutationSampleSize = 6;
    private const int MaxPermutationAttemptsMultiplier = 20;

    private readonly IRouteScorer _routeScorer = routeScorer;
    private readonly ILogger<ClusterFlowRouteSolver> _logger = logger;

    public List<PathPoint> SolveClusterRoute(Grid grid, Cluster cluster, IPathfindingStrategy strategy, List<List<PathPoint>> currentRoutes)
    {
        _logger.LogInformation("Solving cluster {ClusterName}", cluster.Name);

        if (cluster.Stations.Count <= 1)
        {
            return [.. cluster.Stations];
        }

        List<PathPoint>? bestResult = null;
        RoutePlanningScoreDto? bestScore = null;

        IReadOnlyList<IReadOnlyList<Station>> clusterStations = [cluster.Stations];

        foreach (var permutation in GetStationPermutations(cluster.Stations, ClusterPermutationSampleSize))
        {
            // Close the loop by returning to the first station of this permutation.
            List<PathPoint> loopStationsOrder = [.. permutation.Cast<PathPoint>(), permutation[0]];

            var solveResult = strategy.Solve(
                grid,
                loopStationsOrder,
                currentRoutes,
                RouteSolvePurpose.ClusterLoop,
                desiredSolutions: 1);

            List<PathPoint> candidateResult = solveResult[0];
            var score = _routeScorer.GetRouteScore(candidateResult, grid, clusterStations, RouteSolvePurpose.ClusterLoop);

            if (bestScore is null || score.Optimality > bestScore.Optimality)
            {
                bestScore = score;
                bestResult = candidateResult;
            }
        }

        return bestResult!;
    }

    public List<PathPoint> SolveConnectorRoute(Grid grid, Cluster from, Cluster to, IPathfindingStrategy strategy, List<List<PathPoint>> currentRoutes)
    {
        _logger.LogInformation("Solving connector for cluster {SrcClusterName} to {DstClusterName}", from.Name, to.Name);

        var (start, end) = FindNearestConnector(from.Stations, to.Stations);

        var solveResult = strategy.Solve(
            grid,
            [start, end],
            currentRoutes,
            RouteSolvePurpose.Connector,
            desiredSolutions: 1);

        return solveResult[0];
    }

    private static List<List<Station>> GetStationPermutations(IReadOnlyList<Station> stations, int count)
    {
        var random = new Random();
        var seen = new HashSet<string>();
        List<List<Station>> permutations = [];

        int target = Math.Min(count, CountDistinctCycles(stations.Count, count));

        int maxAttempts = target * MaxPermutationAttemptsMultiplier;
        int attempts = 0;

        while (permutations.Count < target && attempts < maxAttempts)
        {
            attempts++;
            List<Station> shuffled = [stations[0], .. stations.Skip(1).OrderBy(_ => random.Next())];
            var signature = string.Join(",", shuffled.Select(s => s.Name));

            if (seen.Add(signature))
            {
                permutations.Add(shuffled);
            }
        }

        return permutations;
    }

    private static int CountDistinctCycles(int stationCount, int cap)
    {
        long cycles = 1;

        for (int i = 2; i < stationCount; i++)
        {
            cycles *= i;

            if (cycles >= cap)
            {
                return cap;
            }
        }

        return (int)cycles;
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
}

using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.RoutePlanning;
using Domain.Missions.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Services.RoutePlanningService;

public class ClusterFlowRouteSolver(IRgvRoutePlanning rgvRoutePlanning, ILogger<ClusterFlowRouteSolver> logger) : IClusterFlowRouteSolver
{
    private const int ClusterGenerationsNumber = 100;
    private const int ConnectorGenerationsNumber = 300;
    private static readonly int? ClusterPermutationSampleSize = 6;

    private readonly IRgvRoutePlanning _rgvRoutePlanning = rgvRoutePlanning;
    private readonly ILogger<ClusterFlowRouteSolver> _logger = logger;

    public List<PathPoint> SolveClusterRoute(Grid grid, Cluster cluster, RoutePlanningAlgorithm algorithm, List<List<PathPoint>> currentRoutes)
    {
        _logger.LogInformation("Solving cluster {ClusterName}", cluster.Name);

        if (cluster.Stations.Count <= 1)
        {
            return [.. cluster.Stations];
        }

        List<PathPoint>? bestResult = null;
        RoutePlanningScoreDto? bestScore = null;

        foreach (var permutation in GetStationPermutations(cluster.Stations, ClusterPermutationSampleSize))
        {
            // Close the loop by returning to the first station of this permutation.
            List<PathPoint> loopStationsOrder = [.. permutation.Cast<PathPoint>(), permutation[0]];

            var solveResult = _rgvRoutePlanning.Solve(
                grid,
                loopStationsOrder,
                currentRoutes,
                algorithm,
                ClusterGenerationsNumber);

            List<PathPoint> candidateResult = [.. solveResult.RawPath];
            var score = _rgvRoutePlanning.GetRouteScore(candidateResult, grid, loopStationsOrder);

            if (bestScore is null || score.Optimality > bestScore.Optimality)
            {
                bestScore = score;
                bestResult = candidateResult;
            }
        }

        return bestResult!;
    }

    public List<PathPoint> SolveConnectorRoute(Grid grid, Cluster from, Cluster to, RoutePlanningAlgorithm algorithm, List<List<PathPoint>> currentRoutes)
    {
        _logger.LogInformation("Solving connector for cluster {SrcClusterName} to {DstClusterName}", from.Name, to.Name);

        var (start, end) = FindNearestConnector(from.Stations, to.Stations);

        var solveResult = _rgvRoutePlanning.Solve(
            grid,
            [start, end],
            currentRoutes,
            algorithm,
            ConnectorGenerationsNumber);

        return [.. solveResult.RawPath];
    }

    private static List<List<Station>> GetStationPermutations(IReadOnlyList<Station> stations, int? maxPermutations)
    {
        if (maxPermutations is null)
        {
            return GetAllPermutations([.. stations]);
        }

        return GetRandomDistinctPermutations([.. stations], maxPermutations.Value);
    }

    private static List<List<Station>> GetAllPermutations(List<Station> stations)
    {
        if (stations.Count <= 1)
        {
            return [stations];
        }

        List<List<Station>> permutations = [];
        for (int i = 0; i < stations.Count; i++)
        {
            var remaining = new List<Station>(stations);
            remaining.RemoveAt(i);

            foreach (var subPermutation in GetAllPermutations(remaining))
            {
                permutations.Add([stations[i], .. subPermutation]);
            }
        }

        return permutations;
    }

    private static List<List<Station>> GetRandomDistinctPermutations(List<Station> stations, int count)
    {
        var random = new Random();
        var seen = new HashSet<string>();
        List<List<Station>> permutations = [];

        int maxAttempts = count * 20;
        int attempts = 0;

        while (permutations.Count < count && attempts < maxAttempts)
        {
            attempts++;
            var shuffled = stations.OrderBy(_ => random.Next()).ToList();
            var signature = string.Join(",", shuffled.Select(s => s.Name));

            if (seen.Add(signature))
            {
                permutations.Add(shuffled);
            }
        }

        return permutations;
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

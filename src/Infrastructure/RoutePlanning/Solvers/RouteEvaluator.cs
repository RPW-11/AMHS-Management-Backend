using Application.DTOs.RoutePlanning;
using Domain.Missions.ValueObjects;

namespace Infrastructure.RoutePlanning.Solvers;

internal static class RouteEvaluator
{
    private const double RgvSpeed = 1; //ms-1
    private const double LoadingUnloadingTime = 15;
    private const int HourInSeconds = 3600;
    private const double ThroughputEfficiencyFactor = 0.90;

    public sealed record RouteMetrics(double SquareLength, double MinHeadwayTime);

    /// <summary>
    /// Metrics for a layout whose stations are grouped into clusters. Every station in a cluster
    /// serves the same process type, so the cluster's stations work on jobs of that type in
    /// parallel: its slowest station sets how long one job takes, and the station count says how
    /// many are handled at once. The system is paced by its slowest cluster.
    /// </summary>
    public static RouteMetrics GetRouteMetrics(Grid grid, IReadOnlyList<IReadOnlyList<Station>> clusterStations)
    {
        double minHeadwayTime = 0;

        foreach (var cluster in clusterStations)
        {
            if (cluster.Count == 0)
            {
                continue;
            }

            double slowestInCluster = 0;
            foreach (var station in cluster)
            {
                double stationTime = station.ProcessingTime + LoadingUnloadingTime;
                if (stationTime > slowestInCluster)
                    slowestInCluster = stationTime;
            }

            double clusterHeadway = slowestInCluster / cluster.Count;
            if (clusterHeadway > minHeadwayTime)
                minHeadwayTime = clusterHeadway;
        }

        return new RouteMetrics(grid.GetSquareLength(), minHeadwayTime);
    }

    /// <summary>
    /// Metrics for a bare list of points, with no cluster grouping available - each station stands
    /// alone as its own single server. Used by the solver, which is handed one segment's stations
    /// without knowing which of them are interchangeable.
    /// </summary>
    public static RouteMetrics GetRouteMetrics(Grid grid, List<PathPoint> stationsOrder)
    {
        double minHeadwayTime = 0;
        foreach (var point in stationsOrder)
        {
            double processingTime = point is Station station ? station.ProcessingTime : 0;
            double stationTime = processingTime + LoadingUnloadingTime;
            if (stationTime > minHeadwayTime)
                minHeadwayTime = stationTime;
        }

        return new RouteMetrics(grid.GetSquareLength(), minHeadwayTime);
    }

    public static RoutePlanningScoreDto GetSolutionScores(List<PathPoint> solution, Grid grid, IReadOnlyList<IReadOnlyList<Station>> clusterStations, RouteFitnessWeights weights) =>
        GetSolutionScores(solution, GetRouteMetrics(grid, clusterStations), weights);

    public static RoutePlanningScoreDto GetSolutionScores(List<PathPoint> solution, RouteMetrics metrics, RouteFitnessWeights weights)
    {
        double trackLength = solution.Count * metrics.SquareLength;
        double travelTime = trackLength / RgvSpeed;

        double minHeadwayTime = metrics.MinHeadwayTime;

        double cycleTimeForPipeline = travelTime + LoadingUnloadingTime;

        int maxRgvs = (int)Math.Floor(cycleTimeForPipeline / minHeadwayTime) + 1;

        double fleetThroughput = maxRgvs * HourInSeconds / cycleTimeForPipeline;
        double bottleneckThroughput = HourInSeconds / minHeadwayTime;

        double totalThroughput = Math.Min(fleetThroughput, bottleneckThroughput) * ThroughputEfficiencyFactor;

        double optimality = weights.ThroughputWeight * totalThroughput + weights.LengthWeight * 1 / trackLength + weights.NumOfRgvsWeight * 1 / maxRgvs;

        return new RoutePlanningScoreDto(totalThroughput, trackLength, maxRgvs, optimality);
    }
}

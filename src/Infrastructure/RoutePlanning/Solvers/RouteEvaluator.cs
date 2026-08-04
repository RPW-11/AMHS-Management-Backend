using Application.DTOs.RoutePlanning;
using Domain.Missions.ValueObjects;

namespace Infrastructure.RoutePlanning.Solvers;

internal static class RouteEvaluator
{
    private const double RgvSpeed = 1; //ms-1
    private const double LoadingUnloadingTime = 15;
    private const int HourInSeconds = 3600;
    private const double ThroughputEfficiencyFactor = 0.90;

    // Derived from the grid and the stations being visited, never from the candidate route, so a
    // caller scoring many solutions against one map builds this once instead of per solution.
    public sealed record RouteMetrics(double SquareLength, double MaxStationTime);

    public static RouteMetrics GetRouteMetrics(Grid grid, List<PathPoint> stationsOrder)
    {
        double maxStationTime = 0;
        foreach (var point in stationsOrder)
        {
            double processingTime = point is Station station ? station.ProcessingTime : 0;
            double stationTime = processingTime + LoadingUnloadingTime;
            if (stationTime > maxStationTime)
                maxStationTime = stationTime;
        }

        return new RouteMetrics(grid.GetSquareLength(), maxStationTime);
    }

    public static RoutePlanningScoreDto GetSolutionScores(List<PathPoint> solution, Grid grid, List<PathPoint> stationsOrder, RouteFitnessWeights weights) =>
        GetSolutionScores(solution, GetRouteMetrics(grid, stationsOrder), weights);

    public static RoutePlanningScoreDto GetSolutionScores(List<PathPoint> solution, RouteMetrics metrics, RouteFitnessWeights weights)
    {
        double trackLength = solution.Count * metrics.SquareLength;
        double travelTime = trackLength / RgvSpeed;

        double minHeadwayTime = metrics.MaxStationTime;

        double cycleTimeForPipeline = travelTime + LoadingUnloadingTime;

        int maxRgvs = (int)Math.Floor(cycleTimeForPipeline / minHeadwayTime) + 1;

        double bottleneckThroughputPerRgv = HourInSeconds / metrics.MaxStationTime;
        double totalThroughput = maxRgvs * bottleneckThroughputPerRgv * ThroughputEfficiencyFactor;

        double optimality = weights.ThroughputWeight * totalThroughput + weights.LengthWeight * 1 / trackLength + weights.NumOfRgvsWeight * 1 / maxRgvs;

        return new RoutePlanningScoreDto(totalThroughput, trackLength, maxRgvs, optimality);
    }
}

using Domain.Missions.ValueObjects;

namespace Infrastructure.RoutePlanning.Rgv;

internal static class RouteEvaluator
{
    private const double RgvSpeed = 1; //ms-1
    private const double ThroughputWeight = 0.8;
    private const double LengthWeight = 0.1;
    private const double NumOfRgvsWeight = 0.1;
    private const double LoadingUnloadingTime = 15;
    private const int HourInSeconds = 3600;
    private const double ThroughputEfficiencyFactor = 0.90;

    public static (double throughput, double trackLength, int numOfRgvs, double optimality) GetSolutionScores(List<PathPoint> solution, Grid grid, List<PathPoint> stationsOrder)
    {
        double trackLength = solution.Count * grid.GetSquareLength();
        double travelTime = trackLength / RgvSpeed;

        double maxStationTime = 0;
        foreach (var point in stationsOrder)
        {
            double processingTime = point is Station station ? station.ProcessingTime : 0;
            double stationTime = processingTime + LoadingUnloadingTime;
            if (stationTime > maxStationTime)
                maxStationTime = stationTime;
        }

        double minHeadwayTime = maxStationTime;

        double cycleTimeForPipeline = travelTime + LoadingUnloadingTime;

        int maxRgvs = (int)Math.Floor(cycleTimeForPipeline / minHeadwayTime) + 1;

        double bottleneckThroughputPerRgv = HourInSeconds / maxStationTime;
        double totalThroughput = maxRgvs * bottleneckThroughputPerRgv * ThroughputEfficiencyFactor;

        double optimality = ThroughputWeight * totalThroughput + LengthWeight * 1 / trackLength + NumOfRgvsWeight * 1 / maxRgvs;

        return (totalThroughput, trackLength, maxRgvs, optimality);
    }
}

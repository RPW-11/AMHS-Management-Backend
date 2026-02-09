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

    public static double EvaluateOptimality(List<PathPoint> solution, RgvMap map)
    {
        var (totalThroughput, trackLength, maxRgvs) = GetSolutionScores(solution, map);
        var finalScore = ThroughputWeight * totalThroughput + LengthWeight * 1 / trackLength + NumOfRgvsWeight * 1 / maxRgvs;

        return finalScore;
    }

    public static (double throughput, double trackLength, int numOfRgvs) GetSolutionScores(List<PathPoint> solution, RgvMap map)
    {
        // BOTTLENECK FOCUS
        double trackLength = solution.Count * map.GetSquareLength();
        double travelTime = trackLength / RgvSpeed;

        // Find the station with maximum occupation time
        double maxStationTime = 0;
        foreach (var station in map.StationsOrder)
        {
            double stationTime = station.Time + LoadingUnloadingTime;
            if (stationTime > maxStationTime)
                maxStationTime = stationTime;
        }

        double minHeadwayTime = maxStationTime;

        double cycleTimeForPipeline = travelTime + LoadingUnloadingTime;

        int maxRgvs = (int)Math.Floor(cycleTimeForPipeline / minHeadwayTime) + 1;

        double bottleneckThroughputPerRgv = HourInSeconds / maxStationTime;
        double totalThroughput = maxRgvs * bottleneckThroughputPerRgv;

        totalThroughput *= 0.90;

        return (totalThroughput, trackLength, maxRgvs);
    }

    public static List<PathPoint> GetBestRoute(List<List<PathPoint>> possibleRoutes, List<PathPoint> stationsOrder, RgvMap map)
    {
        List<double> routesQ = []; // product per hour
        List<int> routesMaxRgvs = []; // max Rgvs
        List<double> routesTrackLength = []; // routes length

        double totalStationsTime = stationsOrder.Sum(s => s.Time);

        foreach (var route in possibleRoutes)
        {
            double trackLength = route.Count * map.GetSquareLength();
            double timePerLoop = (trackLength / RgvSpeed) + totalStationsTime;
            double perRgvQ = 3600 / timePerLoop;

            double rgvAvgSpeed = trackLength / timePerLoop;

            double intermedSpace = totalStationsTime * rgvAvgSpeed;

            int maxRgvs = (int)Math.Floor(trackLength / intermedSpace);

            double totalQ = maxRgvs * perRgvQ; // N product per hour

            routesQ.Add(totalQ);
            routesMaxRgvs.Add(maxRgvs);
            routesTrackLength.Add(trackLength);
        }

        var bestRouteIdx = Enumerable.Range(0, routesQ.Count)
                           .OrderByDescending(i => routesQ[i])
                           .ThenBy(i => routesMaxRgvs[i])
                           .ThenBy(i => routesTrackLength[i])
                           .Take(1).ToArray()[0];

        return possibleRoutes[bestRouteIdx];
    }
}

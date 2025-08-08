using Domain.Mission.ValueObjects;

namespace Infrastructure.RoutePlanning.Rgv;

internal static class RouteEvaluator
{
    public static List<PathPoint> GetBestRoute(List<List<PathPoint>> possibleRoutes, List<PathPoint> stationsOrder)
    {
        List<double> routesQ = []; // product per hour
        List<int> routesMaxRgvs = []; // max Rgvs
        List<double> routesTrackLength = []; // routes length

        double totalStationsTime = stationsOrder.Sum(s => s.Time);

        foreach (var route in possibleRoutes)
        {
            double trackLength = route.Count * RgvConstants.PerSquareLength;
            double timePerLoop = (trackLength / RgvConstants.RgvSpeed) + totalStationsTime;
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

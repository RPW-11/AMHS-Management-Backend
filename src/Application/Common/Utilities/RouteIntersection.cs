using Domain.Missions.ValueObjects;

namespace Application.Common.Utilities;

public static class RouteIntersection
{
    public static List<PathPoint> GetIntersectionPathPoints(List<List<PathPoint>> routes, bool includeSelfIntersections = true)
    {
        if (includeSelfIntersections)
        {
            return [.. routes
                .SelectMany(route => route)
                .GroupBy(point => point)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)];
        }

        // A point only counts as an intersection here if it is shared across
        // at least two distinct routes (repeated visits within the same route don't count).
        return [.. routes
            .SelectMany((route, routeIndex) => route.Distinct().Select(point => (point, routeIndex)))
            .GroupBy(x => x.point)
            .Where(group => group.Select(x => x.routeIndex).Distinct().Count() > 1)
            .Select(group => group.Key)];
    }
}

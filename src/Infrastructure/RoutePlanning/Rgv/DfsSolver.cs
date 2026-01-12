using Domain.Mission.ValueObjects;

namespace Infrastructure.RoutePlanning.Rgv;

internal class DfsSolver
{
    public static List<PathPoint> FindBestRoute(RgvMap map)
    {
        List<List<PathPoint>> possibleRoutes = FindAllPossibleRoutes(map);
        if (possibleRoutes.Count == 0)
        {
            return [];
        }
        
        List<PathPoint> bestRoute = RouteEvaluator.GetBestRoute(possibleRoutes, map.StationsOrder);

        return bestRoute;
    }
    
    private static List<List<PathPoint>> FindAllPossibleRoutes(RgvMap map, int? limit = null)
    {
        List<List<PathPoint>> finalRoutes;
        PathPoint startPoint = map.StationsOrder[0];
        PathPoint goalPoint = map.StationsOrder[1];
        HashSet<PathPoint> visitedPos = GetNewVisitedPos([], map, startPoint, goalPoint);

        List<List<PathPoint>> routes = [];
        List<PathPoint> currRoute = [];

        var rowPos = startPoint.RowPos;
        var colPos = startPoint.ColPos;

        SolveHelper(limit, routes, currRoute, visitedPos, map, rowPos, colPos, goalPoint);

        finalRoutes = [.. routes];

        for (int i = 1; i < map.StationsOrder.Count; i++)
        {
            List<List<PathPoint>> tempFinalRoutes = [];
            int startIdx = i;
            int goalIdx = (i + 1) % map.StationsOrder.Count;

            startPoint = map.StationsOrder[startIdx];
            goalPoint = map.StationsOrder[goalIdx];

            rowPos = startPoint.RowPos;
            colPos = startPoint.ColPos;
            foreach (var route in finalRoutes)
            {
                // update visited pos
                visitedPos = GetNewVisitedPos(route, map, startPoint, goalPoint);
                currRoute = [];
                routes = [];

                SolveHelper(limit, routes, currRoute, visitedPos, map, rowPos, colPos, goalPoint);

                foreach (var intermedRoute in routes)
                {
                    var combinedPath = route.Concat(intermedRoute.Skip(1)).ToList();
                    tempFinalRoutes.Add(combinedPath);
                }
            }

            finalRoutes = [.. tempFinalRoutes];
        }

        return finalRoutes;
    }

    private static void SolveHelper(
        int? limit,
        List<List<PathPoint>> routes,
        List<PathPoint> currRoute,
        HashSet<PathPoint> visitedPos,
        RgvMap map,
        int currRowPos,
        int currColPos,
        PathPoint goalPoint
    )
    {
        PathPoint? currPoint = map.GetPointAt(currRowPos, currColPos);
        if (
            (limit != null && limit == routes.Count) ||
            currPoint is null ||
            currPoint.Category == PathPoint.PointCategory.Obstacle ||
            visitedPos.Contains(currPoint)
        )
        {
            return;
        }

        currRoute.Add(currPoint);
        visitedPos.Add(currPoint);

        if (currPoint == goalPoint)
        {
            List<PathPoint> finalRoute = [.. currRoute];
            routes.Add(finalRoute);
        }
        else
        {
            foreach (var direction in RgvMap.MapTrajectory.AllDirections)
            {
                SolveHelper(limit, routes, currRoute, visitedPos, map, currRowPos + direction[0], currColPos + direction[1], goalPoint);
            }
        }

        visitedPos.Remove(currPoint);
        currRoute.Remove(currPoint);
    }
    private static HashSet<PathPoint> GetNewVisitedPos(
        List<PathPoint> route,
        RgvMap map,
        PathPoint startPoint,
        PathPoint goalPoint
    ) {

        HashSet<PathPoint> visitedPos = [.. route];

        foreach (var point in map.StationsOrder)
        {
            visitedPos.Add(point);
        }

        visitedPos.Remove(startPoint);
        visitedPos.Remove(goalPoint);

        return visitedPos;
    }
}

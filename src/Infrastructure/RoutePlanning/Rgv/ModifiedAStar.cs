using Domain.Mission.ValueObjects;
using static Domain.Mission.ValueObjects.PathPoint;
using static Domain.Mission.ValueObjects.RgvMap;

namespace Infrastructure.RoutePlanning.Rgv;

public static class ModifiedAStar
{
    private const int MaxSolutions = 100;
    private const int MaxSolutionsPerSegment = 10;
    private const double PerStepCost = 1;
    private const double PertubationRate = 0;

    public static List<List<PathPoint>> GetValidSolutions(RgvMap rgvMap)
    {
        Random random = new();
        
        Console.WriteLine("Running Modified A* to find solutions...");
        List<List<List<PathPoint>>> segmentPaths = [];

        for (int i = 0; i < rgvMap.StationsOrder.Count; i++)
        {
            var startPoint = rgvMap.StationsOrder[i];
            var goalPoint = rgvMap.StationsOrder[(i + 1) % rgvMap.StationsOrder.Count];

            Console.WriteLine($"Finding solution for {startPoint} --- {goalPoint}...");

            var solutions = Solve(rgvMap, startPoint, goalPoint, []);
            segmentPaths.Add(solutions);
        }

        List<List<PathPoint>> allPaths = segmentPaths[0];

        for (int i = 1; i < rgvMap.StationsOrder.Count; i++)
        {
            List<List<PathPoint>> tempAllPaths = [];
            List<PathPoint> completePath;
            foreach (var path in allPaths)
            {
                HashSet<PathPoint> occupiedPoints = [];
                UpdateOccupiedPoints(occupiedPoints, path);
                foreach (var nextPath in segmentPaths[i])
                {
                    if (!IsPathIntersect(occupiedPoints, nextPath))
                    {
                        completePath = [.. path, .. nextPath.Skip(1)];
                        tempAllPaths.Add(completePath);
                    }
                }
            }
            allPaths = tempAllPaths;
        }

        int totalSolutions = allPaths.Count;
        Console.WriteLine($"Obtained: {totalSolutions} solutions");

        if (totalSolutions > MaxSolutions)
        {
            return [.. allPaths.OrderBy(x => random.Next()).Take(MaxSolutions)];
        }

        return allPaths;
    }

    private static void UpdateOccupiedPoints(HashSet<PathPoint> occupiedPoints, List<PathPoint> addedPath)
    {
        for (int i = 1; i < addedPath.Count - 1; i++) // ignore the last element
        {
            occupiedPoints.Add(addedPath[i]);
        }
    }

    private static bool IsPathIntersect(HashSet<PathPoint> occupiedPoints, List<PathPoint> newPath)
    {
        foreach (var point in newPath)
        {
            if (occupiedPoints.Contains(point))
            {
                return true;
            }
        }
        return false;
    }
    private static List<List<PathPoint>> Solve(RgvMap rgvMap, PathPoint startPoint, PathPoint goalPoint, HashSet<PathPoint> occupiedPoints)
    {
        var openSet = new PriorityQueue<(PathPoint point, List<PathPoint> path, double gCost), double>();
        var gCosts = new Dictionary<PathPoint, List<(double gCost, List<PathPoint> path)>>();
        var solutions = new List<List<PathPoint>>();
        var random = new Random();

        openSet.Enqueue((startPoint, new List<PathPoint> { startPoint }, 0), 0);
        gCosts[startPoint] = [(0, new List<PathPoint> { startPoint })];

        while (openSet.Count > 0 && solutions.Count < MaxSolutionsPerSegment)
        {
            var (current, path, gCost) = openSet.Dequeue();

            if (current == goalPoint)
            {
                solutions.Add([.. path]);
                continue;
            }

            foreach (var direction in MapTrajectory.AllDirections)
            {
                var neighbor = rgvMap.GetPointAt(current.RowPos + direction[0], current.ColPos + direction[1]);

                if (neighbor is not null && neighbor.Category != PointCategory.Obstacle &&
                    (!occupiedPoints.Contains(neighbor) || neighbor == goalPoint))
                {
                    double tentativeGCost = gCost + PerStepCost;
                    double heuristicScore = ManhattanDistanceHeuristic(neighbor, goalPoint);
                    double fCost = tentativeGCost + heuristicScore + (random.NextDouble() * PertubationRate);

                    var newPath = new List<PathPoint>(path) { neighbor };

                    openSet.Enqueue((neighbor, newPath, tentativeGCost), fCost);

                    if (!gCosts.ContainsKey(neighbor))
                    {
                        gCosts[neighbor] = [];
                    }
                    gCosts[neighbor].Add((tentativeGCost, newPath));
                }
            }
        }

        return solutions;
    }

    private static double ManhattanDistanceHeuristic(PathPoint point1, PathPoint point2)
    {
        return Math.Abs(point1.RowPos - point2.RowPos) + Math.Abs(point1.ColPos - point2.ColPos);
    }
}

using Domain.Mission.ValueObjects;
using static Domain.Mission.ValueObjects.PathPoint;
using static Domain.Mission.ValueObjects.RgvMap;

namespace Infrastructure.RoutePlanning.Rgv;

public static class ModifiedAStar
{
    private const int MaxSolutions = 200;
    private const int MaxSolutionsPerSegment = 3;
    private const double PerStepCost = 1;
    private const double PertubationRate = 0;

    public static List<List<PathPoint>> GetValidSolutions(RgvMap rgvMap)
    {
        var intersectSolutions = GetValidSolutionsIntersect(rgvMap);
        var nonIntersectSolutions = GetValidSolutionsNoIntersect(rgvMap);

        return [.. intersectSolutions, .. nonIntersectSolutions];
    }

    private static List<List<PathPoint>> GetValidSolutionsIntersect(RgvMap rgvMap)
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

        System.Console.WriteLine("Concatenating path...");

        for (int i = 1; i < rgvMap.StationsOrder.Count; i++)
        {
            List<List<PathPoint>> tempAllPaths = [];
            List<PathPoint> completePath;
            foreach (var path in allPaths)
            {
                foreach (var nextPath in segmentPaths[i])
                {
                    completePath = [.. path, .. nextPath.Skip(1)];
                    tempAllPaths.Add(completePath);
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

    private static List<List<PathPoint>> GetValidSolutionsNoIntersect(RgvMap rgvMap)
    {
        Random random = new();
        Console.WriteLine("Running Modified A* with No Intersect to find solutions...");

        // Initial search
        List<List<PathPoint>> possiblePaths = Solve(rgvMap, rgvMap.StationsOrder[0], rgvMap.StationsOrder[1], []);;

        for (int i = 1; i < rgvMap.StationsOrder.Count; i++)
        {
            var startPoint = rgvMap.StationsOrder[i];
            var goalPoint = rgvMap.StationsOrder[(i + 1) % rgvMap.StationsOrder.Count];
            List<List<PathPoint>> tempPaths = [];

            Console.WriteLine($"Finding solution for {startPoint} --- {goalPoint}...");

            foreach (var path in possiblePaths)
            {
                var occupiedPoints = new HashSet<PathPoint>();
                UpdateOccupiedPoints(occupiedPoints, path);
                var solutions = Solve(rgvMap, startPoint, goalPoint, occupiedPoints);
                foreach (var sol in solutions)
                {
                    tempPaths.Add([.. path, .. sol.Skip(1)]);
                }
            }

            possiblePaths = tempPaths;
        }

        int totalSolutions = possiblePaths.Count;
        Console.WriteLine($"Obtained: {totalSolutions} solutions");

        if (totalSolutions > MaxSolutions)
        {
            return [.. possiblePaths.Take(MaxSolutions)];
        }

        return possiblePaths;
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
        var openSet = new PriorityQueue<(PathPoint point, PathPoint parent, double gCost), double>();
        var gCosts = new Dictionary<PathPoint, double>(); // Track best gCost
        var parents = new Dictionary<PathPoint, PathPoint>(); // Track parent for path reconstruction
        var solutions = new List<List<PathPoint>>();
        var random = new Random();
        double bestSolutionCost = double.MaxValue; // Track best solution cost for pruning

        openSet.Enqueue((startPoint, null, 0), 0);
        gCosts[startPoint] = 0;
        parents[startPoint] = null;

        while (openSet.Count > 0 && solutions.Count < MaxSolutionsPerSegment)
        {
            var (current, parent, gCost) = openSet.Dequeue();

            // if (gCosts.ContainsKey(current) && gCost > gCosts[current])
            //     continue;

            if (current == goalPoint)
            {
                var path = ReconstructPath(current, parents);
                solutions.Add(path);
                bestSolutionCost = Math.Min(bestSolutionCost, gCost);
                continue;
            }

            occupiedPoints.Add(current);
            
            foreach (var direction in MapTrajectory.AllDirections)
            {
                var neighbor = rgvMap.GetPointAt(current.RowPos + direction[0], current.ColPos + direction[1]);

                if (neighbor == null || neighbor.Category == PointCategory.Obstacle)
                    continue;

                if (occupiedPoints.Contains(neighbor) && neighbor != goalPoint)
                    continue;

                double tentativeGCost = gCost + PerStepCost;

                if (tentativeGCost > bestSolutionCost * 30)
                {
                    continue;
                }


                double heuristicScore = ManhattanDistanceHeuristic(neighbor, goalPoint);
                double fCost = tentativeGCost + heuristicScore + (random.NextDouble() * PertubationRate);

                // Update if this is a new node or a better path
                if (!gCosts.ContainsKey(neighbor) || tentativeGCost < gCosts[neighbor] || neighbor == goalPoint)
                {
                    gCosts[neighbor] = tentativeGCost;
                    parents[neighbor] = current;
                    openSet.Enqueue((neighbor, current, tentativeGCost), fCost);
                }
            }
        }

        return solutions;
    }

    private static List<PathPoint> ReconstructPath(PathPoint current, Dictionary<PathPoint, PathPoint> parents)
    {
        var path = new List<PathPoint>();
        while (current != null)
        {
            path.Add(current);
            current = parents[current];
        }
        path.Reverse();
        return path;
    }

    private static double ManhattanDistanceHeuristic(PathPoint point1, PathPoint point2)
    {
        return Math.Abs(point1.RowPos - point2.RowPos) + Math.Abs(point1.ColPos - point2.ColPos);
    }
}

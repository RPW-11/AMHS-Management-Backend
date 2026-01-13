using Domain.Mission.ValueObjects;
using static Domain.Mission.ValueObjects.PathPoint;
using static Domain.Mission.ValueObjects.RgvMap;

namespace Infrastructure.RoutePlanning.Rgv;

public static class ModifiedAStar
{
    private const int MaxSolutions = 200;
    private const int MaxSolutionsPerSegment = 3;
    private const double PerStepCost = 1;
    private const double PertubationRate = 1;

    public static List<List<PathPoint>> GetValidSolutions(RgvMap rgvMap)
    {
        var intersectSolutions = GetValidSolutionsIntersect(rgvMap);
        var nonIntersectSolutions = GetValidSolutionsNoIntersect(rgvMap);
        Console.WriteLine(intersectSolutions);
        Console.WriteLine(nonIntersectSolutions);


        return [.. intersectSolutions, .. nonIntersectSolutions];
    }

    private static List<List<PathPoint>> GetValidSolutionsIntersect(RgvMap rgvMap)
    {
        List<List<List<PathPoint>>> segmentPaths = [];

        for (int i = 0; i < rgvMap.StationsOrder.Count; i++) // O(n)
        {
            var startPoint = rgvMap.StationsOrder[i];
            var goalPoint = rgvMap.StationsOrder[(i + 1) % rgvMap.StationsOrder.Count];
            var solutions = Solve(rgvMap, startPoint, goalPoint, []);
            segmentPaths.Add(solutions);
        }

        List<List<PathPoint>> allPaths = segmentPaths[0];

        for (int i = 1; i < rgvMap.StationsOrder.Count; i++) // O(n * m * k)
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

        if (totalSolutions > MaxSolutions)
        {
            Random random = new();

            return [.. allPaths.OrderBy(x => random.Next()).Take(MaxSolutions)];
        }

        return allPaths;
    }

    private static List<List<PathPoint>> GetValidSolutionsNoIntersect(RgvMap rgvMap)
    {
        // Initial search
        List<List<PathPoint>> possiblePaths = Solve(rgvMap, rgvMap.StationsOrder[0], rgvMap.StationsOrder[1], []);

        for (int i = 1; i < rgvMap.StationsOrder.Count; i++) // O (n * m * k)
        {
            var startPoint = rgvMap.StationsOrder[i];
            var goalPoint = rgvMap.StationsOrder[(i + 1) % rgvMap.StationsOrder.Count];
            List<List<PathPoint>> tempPaths = [];

            foreach (var path in possiblePaths)
            {
                var occupiedPoints = new HashSet<PathPoint>();
                UpdateOccupiedPoints(occupiedPoints, path);
                var solutions = Solve(rgvMap, startPoint, goalPoint, occupiedPoints);

                foreach (var sol in solutions)
                {
                    tempPaths.Add([.. path, .. sol.Skip(1)]);
                }

                if (tempPaths.Count > MaxSolutions)
                {
                    Random random = new();

                    tempPaths = [.. tempPaths.OrderBy(x => random.Next()).Take(MaxSolutions)];
                }
            }

            possiblePaths = tempPaths;
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

    private static List<List<PathPoint>> Solve(
        RgvMap rgvMap, 
        PathPoint startPoint, 
        PathPoint goalPoint, 
        HashSet<PathPoint> occupiedPoints,
        double maxCostFactor = 2.5
    )
    {
        var solutions = new List<List<PathPoint>>();
        var random = new Random();

        var openSet = new PriorityQueue<(PathPoint point, double gCost), double>();
        var gCosts = new Dictionary<PathPoint, double>();
        var parents = new Dictionary<PathPoint, PathPoint?>();
        double bestSolutionCost = double.MaxValue;

        openSet.Enqueue((startPoint, 0), 0);
        gCosts[startPoint] = 0;
        parents[startPoint] = null;

        while (openSet.Count > 0 && solutions.Count < MaxSolutionsPerSegment)
        {
            var (current, gCost) = openSet.Dequeue();

            if (current == goalPoint)
            {
                var path = ReconstructPath(current, parents);
                solutions.Add(path);
                bestSolutionCost = Math.Min(bestSolutionCost, gCost);

                continue;
            }
            
            foreach (var direction in MapTrajectory.AllDirections)
            {
                var neighbor = rgvMap.GetPointAt(current.RowPos + direction[0], current.ColPos + direction[1]);

                if (neighbor is null || neighbor.Category == PointCategory.Obstacle)
                {
                    continue;
                }

                if (occupiedPoints.Contains(neighbor) && neighbor != goalPoint)
                {
                    continue;   
                }

                double tentativeGCost = gCost + PerStepCost;

                if (tentativeGCost > bestSolutionCost * maxCostFactor)
                {
                    continue;
                }

                double heuristicScore = ManhattanDistanceHeuristic(neighbor, goalPoint);
                double fCost = tentativeGCost + heuristicScore + (random.NextDouble() * PertubationRate);

                if (!gCosts.TryGetValue(neighbor, out double value) || tentativeGCost < value || neighbor == goalPoint)
                {
                    value = tentativeGCost;
                    gCosts[neighbor] = value;
                    parents[neighbor] = current;
                    openSet.Enqueue((neighbor, tentativeGCost), fCost);
                }
            }
        }

        return solutions;
    }

    private static List<PathPoint> ReconstructPath(PathPoint current, Dictionary<PathPoint, PathPoint?> parents)
    {
        var path = new List<PathPoint>();

        while (current is not null)
        {
            path.Add(current);
            
            if (parents[current] is not PathPoint next)
                break;
                
            current = next;
        }
        
        path.Reverse();
        return path;
    }

    private static double ManhattanDistanceHeuristic(PathPoint point1, PathPoint point2)
    {
        return Math.Abs(point1.RowPos - point2.RowPos) + Math.Abs(point1.ColPos - point2.ColPos);
    }
}

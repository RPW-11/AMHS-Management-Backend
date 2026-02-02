using static Domain.Missions.ValueObjects.PathPoint;
using static Domain.Missions.ValueObjects.RgvMap;
using Domain.Missions.ValueObjects;

namespace Infrastructure.RoutePlanning.Rgv;

public static class ModifiedAStar
{
    private const int MaxSolutions = 200;
    private const double PerStepCost = 1;

    public static List<List<PathPoint>> GetValidSolutions(RgvMap rgvMap)
    {
        var intersectSolutions = GetValidSolutionsIntersect(rgvMap);
        var nonIntersectSolutions = GetValidSolutionsNoIntersect(rgvMap);

        return [.. intersectSolutions, .. nonIntersectSolutions];
    }

    private static List<List<PathPoint>> GetValidSolutionsIntersect(RgvMap rgvMap)
    {
        List<List<List<PathPoint>>> segmentPaths = [];

        for (int i = 0; i < rgvMap.StationsOrder.Count-1; i++) // O(n)
        {
            var startPoint = rgvMap.StationsOrder[i];
            var goalPoint = rgvMap.StationsOrder[(i + 1) % rgvMap.StationsOrder.Count];
            var solutions = SolveMultipleTimes(rgvMap, startPoint, goalPoint, []);
            segmentPaths.Add(solutions);
        }

        List<List<PathPoint>> allPaths = segmentPaths[0];

        for (int i = 1; i < rgvMap.StationsOrder.Count-1; i++) // O(n * m * k)
        {
            List<List<PathPoint>> tempPaths = [];
            List<PathPoint> completePath;

            foreach (var path in allPaths)
            {
                foreach (var nextPath in segmentPaths[i])
                {
                    completePath = [.. path, .. nextPath.Skip(1)];
                    tempPaths.Add(completePath);
                }

                if (tempPaths.Count > MaxSolutions)
                {
                    Random random = new();

                    tempPaths = [.. tempPaths.OrderBy(x => random.Next()).Take(MaxSolutions)];
                }
            }
            
            allPaths = tempPaths;
        }

        return allPaths;
    }

    private static List<List<PathPoint>> GetValidSolutionsNoIntersect(RgvMap rgvMap)
    {
        // Initial search
        List<List<PathPoint>> possiblePaths = SolveMultipleTimes(rgvMap, rgvMap.StationsOrder[0], rgvMap.StationsOrder[1], []);

        for (int i = 1; i < rgvMap.StationsOrder.Count-1; i++) // O (n * m * k)
        {
            var startPoint = rgvMap.StationsOrder[i];
            var goalPoint = rgvMap.StationsOrder[(i + 1) % rgvMap.StationsOrder.Count];
            List<List<PathPoint>> tempPaths = [];

            foreach (var path in possiblePaths)
            {
                var occupiedPoints = new HashSet<PathPoint>();
                UpdateOccupiedPoints(occupiedPoints, path);
                var solutions = SolveMultipleTimes(rgvMap, startPoint, goalPoint, occupiedPoints);

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

    private static List<List<PathPoint>> SolveMultipleTimes(
        RgvMap rgvMap, 
        PathPoint startPoint, 
        PathPoint goalPoint, 
        HashSet<PathPoint> occupiedPoints,
        int desiredSolutions = 8,  
        double maxCostFactor = 2.5)
    {
        var allSolutions = new List<List<PathPoint>>();

        var configurations = new[]
        {
            new { Weight = 1.00, Perturbation = 0.15 },
            new { Weight = 1.35, Perturbation = 0.20 },
            new { Weight = 1.80, Perturbation = 0.25 },
            new { Weight = 2.50, Perturbation = 0.30 },
            new { Weight = 3.50, Perturbation = 0.40 },
        };

        foreach (var config in configurations)
        {
            var solutionsFromThisRun = Solve(
                rgvMap, startPoint, goalPoint, occupiedPoints,
                config.Weight, config.Perturbation,
                maxCostFactor, maxSolutionsPerConfig: 3);

            allSolutions.AddRange(solutionsFromThisRun);

            if (allSolutions.Count >= desiredSolutions)
            {
                var random = new Random();
                allSolutions = [.. allSolutions.OrderBy(x => random.Next()).Take(desiredSolutions)];
            }
        }

        return allSolutions;
    }

    public static List<List<PathPoint>> Solve(
        RgvMap rgvMap, 
        PathPoint startPoint, 
        PathPoint goalPoint, 
        HashSet<PathPoint> occupiedPoints,
        double heuristicWeight = 0.1,
        double perturbationMax = 0.5,
        double maxCostFactor = 2.5,
        int maxSolutionsPerConfig = 2
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

        while (openSet.Count > 0 && solutions.Count < maxSolutionsPerConfig)
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
                double weightedHeuristic = heuristicWeight * heuristicScore;
                double randomPert = (random.NextDouble() * 2 - 1) * perturbationMax;

                double fCost = tentativeGCost + weightedHeuristic + randomPert;

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

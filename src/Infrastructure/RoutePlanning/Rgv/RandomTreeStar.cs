using Domain.Missions.ValueObjects;
using static Domain.Missions.ValueObjects.PathPoint;

namespace Infrastructure.RoutePlanning.Rgv;

public static class RandomTreeStar
{
    private const int MaxSolutions = 100;
    private const int NumVariationsPerSegment = 6;
    private const int maxIterations = 1500;
    private const double stepSize = 3.0;
    private const double rewireRadius = 5.0;
    private const double goalBias = 0.15;

    public static List<List<PathPoint>> GenerateRRTSolutions(RgvMap rgvMap)
    {
        List<List<List<PathPoint>>> segmentPaths = [];

        for (int i = 0; i < rgvMap.StationsOrder.Count-1; i++) // O(n)
        {
            var startPoint = rgvMap.StationsOrder[i];
            var goalPoint = rgvMap.StationsOrder[(i + 1) % rgvMap.StationsOrder.Count];
            var solutions = Solve(rgvMap, startPoint, goalPoint, []);
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

    private static List<List<PathPoint>> Solve(
        RgvMap rgvMap,
        PathPoint start,
        PathPoint goal,
        HashSet<PathPoint> occupiedPoints
    )
    {
        var allPaths = new List<List<PathPoint>>();
        var rand = new Random();

        for (int i = 0; i < NumVariationsPerSegment; i++)
        {
            Console.WriteLine($"Running variation: {i+1} for point: {start} | {goal}");
            var variationRand = new Random(rand.Next());

            var treeNodes = new List<PathPoint> { start };
            var parentMap = new Dictionary<PathPoint, PathPoint?> { { start, null } };
            var costMap = new Dictionary<PathPoint, double> { { start, 0.0 } };

            bool found = false;

            for (int iter = 0; iter < maxIterations; iter++)
            {
                PathPoint sample;
                if (variationRand.NextDouble() < goalBias)
                    sample = goal;
                else
                    sample = GetRandomFreePoint(rgvMap, variationRand);

                // Find nearest node in tree
                PathPoint nearest = treeNodes[0];
                double minDist = Distance(nearest, sample);
                foreach (var node in treeNodes)
                {
                    double d = Distance(node, sample);
                    if (d < minDist)
                    {
                        minDist = d;
                        nearest = node;
                    }
                }

                // Steer: extend toward sample (up to stepSize)
                PathPoint newNode = ExtendToward(nearest, sample, stepSize, rgvMap);

                if (newNode is null || occupiedPoints.Contains(newNode) && !newNode.Equals(goal))
                    continue;

                if (!IsLineFree(rgvMap, nearest, newNode))
                    continue;

                // Find best parent in rewire radius
                double newCost = costMap[nearest] + Distance(nearest, newNode);
                PathPoint bestParent = nearest;

                var nearby = GetNodesInRadius(treeNodes, newNode, rewireRadius);
                foreach (var near in nearby)
                {
                    if (near.Equals(nearest)) continue;
                    double tempCost = costMap[near] + Distance(near, newNode);
                    if (tempCost < newCost && IsLineFree(rgvMap, near, newNode))
                    {
                        newCost = tempCost;
                        bestParent = near;
                    }
                }

                treeNodes.Add(newNode);
                parentMap[newNode] = bestParent;
                costMap[newNode] = newCost;

                foreach (var near in nearby)
                {
                    if (near.Equals(bestParent)) continue;
                    double rewiredCost = newCost + Distance(newNode, near);
                    if (rewiredCost < costMap[near] && IsLineFree(rgvMap, newNode, near))
                    {
                        parentMap[near] = newNode;
                        costMap[near] = rewiredCost;
                    }
                }

                // Check if close to goal
                if (Distance(newNode, goal) <= stepSize * 1.5)  // loose tolerance
                {
                    // Reconnect to goal if better
                    if (IsLineFree(rgvMap, newNode, goal))
                    {
                        double goalCost = costMap[newNode] + Distance(newNode, goal);
                        if (!costMap.TryGetValue(goal, out double value) || goalCost < value)
                        {
                            parentMap[goal] = newNode;
                            value = goalCost;
                            costMap[goal] = value;
                        }
                    }

                    if (parentMap.ContainsKey(goal))
                    {
                        var path = ReconstructRRTPath(parentMap, goal);
                        allPaths.Add(path);
                        found = true;
                        break;  // one path per variation; remove if want multiple per run
                    }
                }
            }

            if (!found)
            {
                Console.WriteLine($"No path is found for variation: {i}");
            }
        }

        return allPaths;
    }

    private static PathPoint GetRandomFreePoint(RgvMap rgvMap, Random rand)
    {
        while (true)
        {
            int row = rand.Next(rgvMap.RowDim); 
            int col = rand.Next(rgvMap.ColDim);
            var pt = rgvMap.GetPointAt(row, col);

            if (pt is not null && pt.Category != PointCategory.Obstacle)
                return pt;
        }
    }

    private static double Distance (PathPoint a, PathPoint b)
    {
        int dx = a.ColPos - b.ColPos;
        int dy = a.RowPos - b.RowPos;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static bool IsLineFree(RgvMap map, PathPoint a, PathPoint b)
    {
        int dx = Math.Abs(b.ColPos - a.ColPos);
        int dy = Math.Abs(b.RowPos - a.RowPos);
        int sx = a.ColPos < b.ColPos ? 1 : -1;
        int sy = a.RowPos < b.RowPos ? 1 : -1;
        int err = dx - dy;

        PathPoint current = a;
        while (true)
        {
            if (current.Category == PointCategory.Obstacle)
                return false;

            if (current.Equals(b))
                return true;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; current = map.GetPointAt(current.RowPos, current.ColPos + sx) ?? current; }
            if (e2 < dx)  { err += dx; current = map.GetPointAt(current.RowPos + sy, current.ColPos) ?? current; }
        }
    }

    private static PathPoint ExtendToward(PathPoint near, PathPoint target, double maxDist, RgvMap map)
    {
        double dist = Distance(near, target);
        if (dist <= maxDist) return target;

        double ratio = maxDist / dist;
        int newRow = near.RowPos + (int)Math.Round((target.RowPos - near.RowPos) * ratio);
        int newCol = near.ColPos + (int)Math.Round((target.ColPos - near.ColPos) * ratio);

        return map.GetPointAt(newRow, newCol) ?? near;
    }

    private static List<PathPoint> GetNodesInRadius(List<PathPoint> nodes, PathPoint center, double radius)
    {
        var nearby = new List<PathPoint>();
        foreach (var n in nodes)
        {
            if (Distance(n, center) <= radius)
                nearby.Add(n);
        }
        return nearby;
    }

    private static List<PathPoint> ReconstructRRTPath(Dictionary<PathPoint, PathPoint?> parentMap, PathPoint end)
    {
        var path = new List<PathPoint>();
        var current = end;
        int step = 0;
        const int MAX_STEPS = 10000;

        while (current is not null && step < MAX_STEPS)
        {
            path.Add(current);

            if (!parentMap.TryGetValue(current, out PathPoint? parent))
            {
                Console.WriteLine($"→ No parent found for {current} → stopping");
                break;
            }

            if (parent is not null && parent == current) // self-loop protection (should never happen)
            {
                Console.WriteLine("→ Self-loop detected!");
                break;
            }

            current = parent;
            step++;
        }

        if (step >= MAX_STEPS)
            Console.WriteLine("→ Reached max steps — possible cycle!");

        path.Reverse();
        return path;
    }
}


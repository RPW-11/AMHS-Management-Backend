using Domain.Missions.ValueObjects;

namespace Infrastructure.RoutePlanning.Rgv;

public static class RandomTreeStar
{
    private const int MaxSolutions = 100;
    private const int NumVariationsPerSegment = 6;
    private const int MaxIterations = 1500;
    private const double StepSize = 3.0;
    private const double RewireRadius = 5.0;
    private const double GoalBias = 0.15;

    public static List<List<PathPoint>> GenerateRRTSolutions(Grid grid, List<PathPoint> stationsOrder)
    {
        List<List<List<PathPoint>>> segmentPaths = [];

        for (int i = 0; i < stationsOrder.Count - 1; i++) // O(n)
        {
            var startPoint = stationsOrder[i];
            var goalPoint = stationsOrder[(i + 1) % stationsOrder.Count];
            var solutions = Solve(grid, startPoint, goalPoint, []);
            segmentPaths.Add(solutions);
        }

        List<List<PathPoint>> allPaths = segmentPaths[0];

        for (int i = 1; i < stationsOrder.Count - 1; i++) // O(n * m * k)
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
        Grid grid,
        PathPoint start,
        PathPoint goal,
        HashSet<PathPoint> occupiedPoints
    )
    {
        var allPaths = new List<List<PathPoint>>();
        var rand = new Random();

        for (int i = 0; i < NumVariationsPerSegment; i++)
        {
            var variationRand = new Random(rand.Next());

            var treeNodes = new List<PathPoint> { start };
            var parentMap = new Dictionary<PathPoint, PathPoint?> { { start, null } };
            var costMap = new Dictionary<PathPoint, double> { { start, 0.0 } };

            bool found = false;

            for (int iter = 0; iter < MaxIterations; iter++)
            {
                PathPoint sample;
                if (variationRand.NextDouble() < GoalBias)
                    sample = goal;
                else
                    sample = GetRandomFreePoint(grid, variationRand);

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
                PathPoint newNode = ExtendToward(nearest, sample, StepSize, grid);

                if (newNode is null || occupiedPoints.Contains(newNode) && !newNode.Equals(goal))
                    continue;

                if (!IsLineFree(grid, nearest, newNode))
                    continue;

                // Find best parent in rewire radius
                double newCost = costMap[nearest] + Distance(nearest, newNode);
                PathPoint bestParent = nearest;

                var nearby = GetNodesInRadius(treeNodes, newNode, RewireRadius);
                foreach (var near in nearby)
                {
                    if (near.Equals(nearest)) continue;
                    double tempCost = costMap[near] + Distance(near, newNode);
                    if (tempCost < newCost && IsLineFree(grid, near, newNode))
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
                    if (rewiredCost < costMap[near] && IsLineFree(grid, newNode, near))
                    {
                        parentMap[near] = newNode;
                        costMap[near] = rewiredCost;
                    }
                }

                // Check if close to goal
                if (Distance(newNode, goal) <= StepSize * 1.5)
                {
                    // Reconnect to goal if better
                    if (IsLineFree(grid, newNode, goal))
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
                        break;
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

    private static PathPoint GetRandomFreePoint(Grid grid, Random rand)
    {
        while (true)
        {
            int row = rand.Next(grid.RowDim);
            int col = rand.Next(grid.ColDim);
            var pt = grid.GetPointAt(row, col);

            if (pt is not null && pt is not Obstacle)
                return pt;
        }
    }

    private static double Distance(PathPoint a, PathPoint b)
    {
        int dx = a.ColPos - b.ColPos;
        int dy = a.RowPos - b.RowPos;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static bool IsLineFree(Grid grid, PathPoint a, PathPoint b)
    {
        int dx = Math.Abs(b.ColPos - a.ColPos);
        int dy = Math.Abs(b.RowPos - a.RowPos);
        int sx = a.ColPos < b.ColPos ? 1 : -1;
        int sy = a.RowPos < b.RowPos ? 1 : -1;
        int err = dx - dy;

        PathPoint current = a;
        while (true)
        {
            if (current is Obstacle)
                return false;

            if (current.Equals(b))
                return true;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; current = grid.GetPointAt(current.RowPos, current.ColPos + sx) ?? current; }
            if (e2 < dx) { err += dx; current = grid.GetPointAt(current.RowPos + sy, current.ColPos) ?? current; }
        }
    }

    private static PathPoint ExtendToward(PathPoint near, PathPoint target, double maxDist, Grid grid)
    {
        double dist = Distance(near, target);
        if (dist <= maxDist) return target;

        double ratio = maxDist / dist;
        int newRow = near.RowPos + (int)Math.Round((target.RowPos - near.RowPos) * ratio);
        int newCol = near.ColPos + (int)Math.Round((target.ColPos - near.ColPos) * ratio);

        return grid.GetPointAt(newRow, newCol) ?? near;
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
                break;
            }

            if (parent is not null && parent == current)
            {
                break;
            }

            current = parent;
            step++;
        }

        path.Reverse();
        return path;
    }
}

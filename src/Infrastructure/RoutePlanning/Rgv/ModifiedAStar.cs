using static Domain.Missions.ValueObjects.Grid;
using Domain.Missions.ValueObjects;

namespace Infrastructure.RoutePlanning.Rgv;

public static class ModifiedAStar
{
    private const int MaxSolutions = 400;
    private const double PerStepCost = 1;
    private const int BaseDesiredSolutions = 100;

    public static List<List<PathPoint>> GetValidSolutions(Grid grid, List<PathPoint> stationsOrder)
    {
        var intersectSolutions = GetValidSolutionsIntersect(grid, stationsOrder);
        var nonIntersectSolutions = GetValidSolutionsNoIntersect(grid, stationsOrder);

        return [.. intersectSolutions, .. nonIntersectSolutions];
    }

    private static List<List<PathPoint>> GetValidSolutionsIntersect(Grid grid, List<PathPoint> stationsOrder)
    {
        int numSegments = stationsOrder.Count - 1;
        int desiredSolutionsPerSegment = Math.Max(1, BaseDesiredSolutions / numSegments);

        List<List<List<PathPoint>>> segmentPaths = [];

        for (int i = 0; i < stationsOrder.Count - 1; i++) // O(n)
        {
            var startPoint = stationsOrder[i];
            var goalPoint = stationsOrder[(i + 1) % stationsOrder.Count];
            var solutions = SolveMultipleTimes(grid, startPoint, goalPoint, [], desiredSolutionsPerSegment);
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

    private static List<List<PathPoint>> GetValidSolutionsNoIntersect(Grid grid, List<PathPoint> stationsOrder)
    {
        int numSegments = stationsOrder.Count - 1;
        int desiredSolutionsPerSegment = Math.Max(1, BaseDesiredSolutions / numSegments);

        // Initial search
        List<List<PathPoint>> possiblePaths = SolveMultipleTimes(grid, stationsOrder[0], stationsOrder[1], [], desiredSolutionsPerSegment);

        for (int i = 1; i < stationsOrder.Count - 1; i++) // O (n * m * k)
        {
            var startPoint = stationsOrder[i];
            var goalPoint = stationsOrder[(i + 1) % stationsOrder.Count];
            List<List<PathPoint>> tempPaths = [];

            foreach (var path in possiblePaths)
            {
                var occupiedPoints = new HashSet<PathPoint>();
                UpdateOccupiedPoints(occupiedPoints, path);
                var solutions = SolveMultipleTimes(grid, startPoint, goalPoint, occupiedPoints, desiredSolutionsPerSegment);

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
        Grid grid,
        PathPoint startPoint,
        PathPoint goalPoint,
        HashSet<PathPoint> occupiedPoints,
        int desiredSolutions = 8)
    {
        var allSolutions = new List<List<PathPoint>>();

        // Slow-decay configs wander more, so repeated runs yield genuinely different
        // paths; fast-decay configs converge to nearly the same path regardless of seed.
        // Runs scale proportionally with desiredSolutions instead of a fixed count, so a
        // single-segment stationsOrder can generate far more candidates than a many-segment one.
        var configurations = new[]
        {
            new { DecayRate = 0.05, Perturbation = 50.0, Weight = 0.25 },   // Long wandering paths
            new { DecayRate = 1.0, Perturbation = 20.0, Weight = 0.25 },   // Moderate exploration
            new { DecayRate = 3.0, Perturbation = 10.0, Weight = 0.25 },   // Balanced
            new { DecayRate = 10.0, Perturbation = 5.0, Weight = 0.125 },  // Near-optimal with slight variation
            new { DecayRate = 50.0, Perturbation = 0.5, Weight = 0.125 }, // Essentially pure A*
        };

        foreach (var config in configurations)
        {
            int runs = Math.Max(1, (int)Math.Round(desiredSolutions * config.Weight));
            List<int> lengths = [];

            for (int i = 0; i < runs; i++)
            {
                var solution = SolveWithDecay(
                    grid, startPoint, goalPoint, occupiedPoints,
                    decayRate: config.DecayRate,
                    initialPerturbation: config.Perturbation);

                if (solution is not null)
                {
                    allSolutions.Add(solution);
                    lengths.Add(solution.Count);
                }

                if (allSolutions.Count >= desiredSolutions)
                {
                    return allSolutions;
                }
            }
        }

        return allSolutions;
    }

    public static List<PathPoint>? SolveWithDecay(
        Grid grid,
        PathPoint startPoint,
        PathPoint goalPoint,
        HashSet<PathPoint> occupiedPoints,
        double initialWeight = 0.05,
        double finalWeight = 1.5,
        double initialPerturbation = 10.0,
        double decayRate = 2.0)
    {
        double baseManhattan = ManhattanDistanceHeuristic(startPoint, goalPoint);

        if (baseManhattan == 0)
        {
            return [startPoint];
        }

        var random = new Random();

        var openSet = new PriorityQueue<(PathPoint point, double gCost), double>();
        var gCosts = new Dictionary<PathPoint, double>();
        var parents = new Dictionary<PathPoint, PathPoint?>();

        openSet.Enqueue((startPoint, 0), 0);
        gCosts[startPoint] = 0;
        parents[startPoint] = null;

        while (openSet.Count > 0)
        {
            var (current, gCost) = openSet.Dequeue();

            if (current == goalPoint)
            {
                return ReconstructPath(current, parents);
            }

            // Exponential decay tied to search progress: early on the search behaves like
            // broad, Dijkstra-like exploration with high randomness; as gCost approaches
            // (or exceeds) the Manhattan distance it converges to a deterministic greedy
            // beeline toward the goal, guaranteeing the search terminates.
            double progress = gCost / baseManhattan;
            double decay = Math.Exp(-decayRate * progress);
            double currentWeight = initialWeight + (finalWeight - initialWeight) * (1 - decay);
            double currentPerturbation = initialPerturbation * decay;

            foreach (var direction in MapTrajectory.AllDirections)
            {
                var neighbor = grid.GetPointAt(current.RowPos + direction[0], current.ColPos + direction[1]);

                if (neighbor is null || neighbor is Obstacle)
                {
                    continue;
                }

                if (occupiedPoints.Contains(neighbor) && neighbor != goalPoint)
                {
                    continue;
                }

                double tentativeGCost = gCost + PerStepCost;

                double heuristicScore = ManhattanDistanceHeuristic(neighbor, goalPoint);
                double perturbation = (random.NextDouble() * 2 - 1) * currentPerturbation;
                double fCost = tentativeGCost + currentWeight * heuristicScore + perturbation;

                // First-visit-wins: once a node is claimed, it is never re-parented even if a
                // shorter route is found later. This lets the perturbed frontier order itself
                // shape the resulting path, instead of always collapsing to the shortest route.
                if (!gCosts.ContainsKey(neighbor))
                {
                    gCosts[neighbor] = tentativeGCost;
                    parents[neighbor] = current;
                    openSet.Enqueue((neighbor, tentativeGCost), fCost);
                }
            }
        }

        return null;
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

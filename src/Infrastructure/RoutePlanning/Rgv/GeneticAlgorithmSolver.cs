using Domain.Missions.ValueObjects;
using static Domain.Missions.ValueObjects.RgvMap;

namespace Infrastructure.RoutePlanning.Rgv;

public class GeneticAlgorithmSolver
{
    private const int PopulationSize = 400;
    private const int MaxGenerations = 400;
    private const double MutationRate = 0.05;
    private const double CrossoverRate = 0.7;
    private const int ChromosomeLength = 1000;
    private const double DuplicateRoutePenaltyRate = 20;
    private const double TurnPenaltyRate = 20;
    private const double ConflictPenaltyRate = 100;

    private readonly Random _random;
    private readonly RgvMap _rgvMap;
    private readonly List<List<PathPoint>> _currentRoutes;
    private readonly int _goalCount = 0; // it is possible to have multiple goals in the map (goals visited multiple times)

    public GeneticAlgorithmSolver(RgvMap rgvMap, List<List<PathPoint>> currentRoutes)
    {
        _random = new Random();
        _rgvMap = rgvMap;
        _currentRoutes = currentRoutes;
        _goalCount = _rgvMap.StationsOrder.Count(point => point == _rgvMap.StationsOrder.Last());
    }

    public List<PathPoint> Solve()
    {
        var aStarSolutions = ModifiedAStar.GetValidSolutions(_rgvMap);
        var rrtSolutions = RandomTreeStar.GenerateRRTSolutions(_rgvMap);
        var population = Enumerable.Range(0, PopulationSize)
                        .Select(_ => GenerateIndividual())
                        .ToList();

        population.AddRange(aStarSolutions);
        population.AddRange(rrtSolutions);

        for (int i = 0; i < MaxGenerations; i++)
        {
            var evaluated = population.Select(ind => new
            {
                Individual = ind,
                Fitness = EvaluateFitness(ind),
            })
            .OrderByDescending(x => x.Fitness)
            .ToList();

            var bestSolution = evaluated.First();

            Console.WriteLine($"Generation {i + 1}: Best Fitness = {bestSolution.Fitness} | Solution Length = {bestSolution.Individual.Count}");

            List<List<PathPoint>> newPopulation = GenerateNewPopulationFromParents(
                [.. evaluated.Select(x => x.Individual)]
            );

            population = newPopulation;
        }

        var bestIndividual = population.Select(ind => new
        {
            Individual = ind,
            Fitness = EvaluateFitness(ind),
        })
            .OrderByDescending(x => x.Fitness)
            .First();

        return bestIndividual.Individual;
    }

    private List<List<PathPoint>> GenerateNewPopulationFromParents(List<List<PathPoint>> sortedParents)
    {
        List<List<PathPoint>> newPopulation = [];

        // Keep 10% best individuals
        newPopulation.AddRange(sortedParents.Take(PopulationSize / 10));

        while (newPopulation.Count < PopulationSize)
        {
            List<PathPoint> parent1 = TournamentSelection(sortedParents);
            List<PathPoint> parent2 = TournamentSelection(sortedParents);

            // Crossover
            List<PathPoint> child;

            if (_random.NextDouble() < CrossoverRate)
            {
                child = CrossOver(parent1, parent2);
            }
            else
            {
                child = _random.NextDouble() < 0.5 ? parent1 : parent2;
            }

            if (_random.NextDouble() > MutationRate)
            {
                child = Mutate(child);
            }

            newPopulation.Add(child);
        }

        return newPopulation;
    }

    private List<PathPoint> CrossOver(List<PathPoint> parent1, List<PathPoint> parent2)
    {
        var commonPositions = parent1.Intersect(parent2).ToList();

        if (commonPositions.Count == 0)
            return _random.NextDouble() < 0.5 ? parent1 : parent2;

        var crossOverPoint = commonPositions[_random.Next(commonPositions.Count)];

        var index1 = parent1.IndexOf(crossOverPoint);
        var index2 = parent2.IndexOf(crossOverPoint);


        var child = parent1.Take(index1 + 1).ToList();
        child.AddRange(parent2.Skip(index2 + 1));

        if (child.Count > ChromosomeLength)
        {
            child = [.. child.Take(ChromosomeLength)];
        }

        return child;
    }

    private List<PathPoint> Mutate(List<PathPoint> child)
    {
        int startIdx = (int)_random.NextInt64(0, Math.Max(0, child.Count - 10));
        int endIdx = (int)_random.NextInt64(Math.Min(child.Count - 1, startIdx + 5), child.Count - 1);

        var startPoint = child[startIdx];
        var endPoint = child[endIdx];

        // Re-compute alternative path between these points
        var subPaths = ModifiedAStar.Solve(_rgvMap, startPoint, endPoint, [], maxSolutionsPerConfig: 1, perturbationMax: 10);
        if (subPaths is null || subPaths.Count == 0)
        {
            return child;
        }

        return [.. child.Take(startIdx), .. subPaths[0], .. child.Skip(endIdx + 1)];
    }

    private List<PathPoint> TournamentSelection(List<List<PathPoint>> population)
    {
        return population.OrderBy(x => _random.Next())
        .Take(5)
        .OrderByDescending(EvaluateFitness)
        .First();
    }

    private List<PathPoint> GenerateIndividual()
    {
        var start = _rgvMap.StationsOrder[0];
        var goal = _rgvMap.StationsOrder.Last();

        List<PathPoint> route = [start];
        int currentLength = 1;

        while (currentLength < ChromosomeLength)
        {
            var last = route.Last();

            if (last == goal)
            {
                break;
            }

            var neighbors = GetValidNeighbors(last)
                .Where(n => !route.Contains(n))
                .ToList();

            if (neighbors.Count == 0)
            {
                break;
            }

            route.Add(neighbors[_random.Next(neighbors.Count)]);
            currentLength++;
        }

        return route;
    }

    private List<PathPoint> GetValidNeighbors(PathPoint point)
    {
        var validNeighbors = new List<PathPoint>();
        foreach (var direction in MapTrajectory.AllDirections)
        {
            var neighbor = _rgvMap.GetPointAt(point.RowPos + direction[0], point.ColPos + direction[1]);

            if (neighbor is null)
            {
                continue;
            }
            validNeighbors.Add(neighbor);
        }
        return validNeighbors;
    }

    private double EvaluateFitness(List<PathPoint> solution)
    {
        // Fitness formula is defined based on the throughput and validity of the path
        // Check if the solution has the correct order, if not return 0
        if (!IsOrderCorrect(solution)
            || !IsPathConnected(solution)
            || IsPathUsingObstacles(solution))
        {
            return int.MinValue;
        }

        int numOfDuplicates = CountDuplicates(solution);
        int numOfTurns = CountPathTurns(solution);
        int numOfConflicts = CountConflictingDirection(solution);

        return RouteEvaluator.EvaluateOptimality(solution, _rgvMap) - DuplicateRoutePenaltyRate * numOfDuplicates - TurnPenaltyRate * numOfTurns - ConflictPenaltyRate * numOfConflicts;
    }

    private bool IsOrderCorrect(List<PathPoint> solution)
    {
        int startIdx = 0;
        bool valid = false;
        int goalVisitedCount = 0;

        foreach (var point in solution)
        {
            if (point == _rgvMap.StationsOrder.Last())
            {
                goalVisitedCount++;
            }

            if (_rgvMap.StationsOrder[startIdx] == point)
            {
                startIdx++;
                if (startIdx == _rgvMap.StationsOrder.Count)
                {
                    valid = true;
                    break;
                }
            }
        }

        if (goalVisitedCount != _goalCount)
        {
            return false;
        }

        if (valid && _rgvMap.StationsOrder.Last() != solution.Last())
        {
            valid = false;
        }

        return valid;
    }

    private static bool IsPathConnected(List<PathPoint> solution)
    {
        for (int i = 1; i < solution.Count; i++)
        {
            var currPoint = solution[i];
            var prevPoint = solution[i - 1];

            int rowDiff = Math.Abs(currPoint.RowPos - prevPoint.RowPos);
            int colDiff = Math.Abs(currPoint.ColPos - prevPoint.ColPos);

            if (rowDiff + colDiff != 1)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsPathUsingObstacles(List<PathPoint> solution)
    {
        foreach (var point in solution)
        {
            if (point.Category == PathPoint.PointCategory.Obstacle)
            {
                return true;
            }
        }

        return false;
    }

    private static int CountDuplicates(List<PathPoint> solution)
    {
        var hashset = solution.ToHashSet();
        return solution.Count - 1 - hashset.Count; // excluding the start which is visited twice (cycle)
    }

    private static int CountPathTurns(List<PathPoint> solution)
    {
        int turns = 0;

        for (int i = 2; i < solution.Count; i++)
        {
            var prevDirection = (solution[i - 1].RowPos - solution[i - 2].RowPos,
                                 solution[i - 1].ColPos - solution[i - 2].ColPos);
            var currDirection = (solution[i].RowPos - solution[i - 1].RowPos,
                                 solution[i].ColPos - solution[i - 1].ColPos);

            if (prevDirection != currDirection)
            {
                turns++;
            }
        }

        return turns;
    }

    private int CountConflictingDirection(List<PathPoint> solution)
    {
        int conflicts = 0;
        var reversedSolution = solution.AsEnumerable().Reverse().ToList();

        foreach (var route in _currentRoutes)
        {
            conflicts += LongestCommonSubsequence(reversedSolution, route);
        }
        return conflicts;
    }

    private static int LongestCommonSubsequence(List<PathPoint> solution1, List<PathPoint> solution2)
    {
        int m = solution1.Count;
        int n = solution2.Count;

        int[,] dp = new int[m + 1, n + 1];

        for (int i = 1; i <= m; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                if (solution1[i - 1] == solution2[j - 1])
                {
                    dp[i, j] = 1 + dp[i - 1, j - 1];
                }
                else
                {
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                }
            }
        }

        return dp[m, n];
    }
}

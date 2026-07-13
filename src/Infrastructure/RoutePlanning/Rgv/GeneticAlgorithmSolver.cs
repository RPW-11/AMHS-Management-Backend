using Domain.Missions.ValueObjects;
using static Domain.Missions.ValueObjects.Grid;

namespace Infrastructure.RoutePlanning.Rgv;

public class GeneticAlgorithmSolver
{
    private const int PopulationSize = 400;
    private const double MutationRate = 0.05;
    private const double CrossoverRate = 0.7;
    private const int ChromosomeLength = 1000;
    private const double DuplicateRoutePenaltyRate = 1600;
    private const double TurnPenaltyRate = 1000;
    private const double ConflictPenaltyRate = 4000;
    private const int EarlyStoppingPatience = 50;
    private const double ElitismRate = 0.1;
    private const int TournamentSize = 5;
    private const int MutationStartIndexMargin = 10;
    private const int MutationMinSegmentLength = 5;

    private readonly Random _random;
    private readonly Grid _grid;
    private readonly List<PathPoint> _stationsOrder;
    private readonly List<List<PathPoint>> _currentRoutes;
    private readonly int _generationsNumber;
    private readonly int _goalCount = 0;

    public GeneticAlgorithmSolver(Grid grid, List<PathPoint> stationsOrder, List<List<PathPoint>> currentRoutes, int generationsNumber)
    {
        _random = new Random();
        _grid = grid;
        _stationsOrder = stationsOrder;
        _currentRoutes = currentRoutes;
        _generationsNumber = generationsNumber;
        _goalCount = _stationsOrder.Count(point => point == _stationsOrder.Last());
    }

    public List<PathPoint> Solve()
    {
        var aStarSolutions = ModifiedAStar.GetValidSolutions(_grid, _stationsOrder);

        var rrtSolutions = RandomTreeStar.GenerateRRTSolutions(_grid, _stationsOrder);
        var population = Enumerable.Range(0, PopulationSize)
                        .Select(_ => GenerateIndividual())
                        .ToList();

        population.AddRange(aStarSolutions);
        population.AddRange(rrtSolutions);

        double bestFitnessSoFar = double.MinValue;
        int generationsSinceImprovement = 0;

        for (int i = 0; i < _generationsNumber; i++)
        {
            var evaluated = population.Select(ind => new
            {
                Individual = ind,
                Fitness = EvaluateFitness(ind),
            })
            .OrderByDescending(x => x.Fitness)
            .ToList();

            Console.WriteLine($"[GA] Generation {i}: best solution count = {evaluated[0].Individual.Count}, fitness = {evaluated[0].Fitness}");

            if (evaluated[0].Fitness > bestFitnessSoFar)
            {
                bestFitnessSoFar = evaluated[0].Fitness;
                generationsSinceImprovement = 0;
            }
            else if (++generationsSinceImprovement >= EarlyStoppingPatience)
            {
                Console.WriteLine($"[GA] Early stopping at generation {i}: no improvement for {EarlyStoppingPatience} generations");
                population = [.. evaluated.Select(x => x.Individual)];
                break;
            }

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

        Console.WriteLine($"[GA] Best solution: count = {bestIndividual.Individual.Count}, fitness = {bestIndividual.Fitness}");

        return bestIndividual.Individual;
    }

    private List<List<PathPoint>> GenerateNewPopulationFromParents(List<List<PathPoint>> sortedParents)
    {
        List<List<PathPoint>> newPopulation = [];

        newPopulation.AddRange(sortedParents.Take((int)(PopulationSize * ElitismRate)));

        while (newPopulation.Count < PopulationSize)
        {
            List<PathPoint> parent1 = TournamentSelection(sortedParents);
            List<PathPoint> parent2 = TournamentSelection(sortedParents);

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
        int startIdx = (int)_random.NextInt64(0, Math.Max(0, child.Count - MutationStartIndexMargin));
        int endIdx = (int)_random.NextInt64(Math.Min(child.Count - 1, startIdx + MutationMinSegmentLength), child.Count - 1);

        var startPoint = child[startIdx];
        var endPoint = child[endIdx];

        var subPath = ModifiedAStar.SolveWithDecay(_grid, startPoint, endPoint, []);
        if (subPath is null)
        {
            return child;
        }

        return [.. child.Take(startIdx), .. subPath, .. child.Skip(endIdx + 1)];
    }

    private List<PathPoint> TournamentSelection(List<List<PathPoint>> population)
    {
        return population.OrderBy(x => _random.Next())
        .Take(TournamentSize)
        .OrderByDescending(EvaluateFitness)
        .First();
    }

    private List<PathPoint> GenerateIndividual()
    {
        var start = _stationsOrder[0];
        var goal = _stationsOrder.Last();

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
            var neighbor = _grid.GetPointAt(point.RowPos + direction[0], point.ColPos + direction[1]);

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
        // Invalid solutions are disqualified via a minimum fitness so ranking never selects them.
        if (!IsOrderCorrect(solution)
            || !IsPathConnected(solution)
            || IsPathUsingObstacles(solution))
        {
            return int.MinValue;
        }

        int length = Math.Max(1, solution.Count);
        double duplicateRate = (double)CountDuplicates(solution) / length;
        double turnRate = (double)CountPathTurns(solution) / length;
        double conflictRate = CountConflictingDirectionRate(solution);

        return RouteEvaluator.GetSolutionScores(solution, _grid, _stationsOrder).optimality
            - DuplicateRoutePenaltyRate * duplicateRate
            - TurnPenaltyRate * turnRate
            - ConflictPenaltyRate * conflictRate;
    }

    private bool IsOrderCorrect(List<PathPoint> solution)
    {
        int startIdx = 0;
        bool valid = false;
        int goalVisitedCount = 0;

        foreach (var point in solution)
        {
            if (point == _stationsOrder.Last())
            {
                goalVisitedCount++;
            }

            if (_stationsOrder[startIdx] == point)
            {
                startIdx++;
                if (startIdx == _stationsOrder.Count)
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

        if (valid && _stationsOrder.Last() != solution.Last())
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
            if (point is Obstacle)
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

    private double CountConflictingDirectionRate(List<PathPoint> solution)
    {
        double totalRate = 0;
        var reversedSolution = solution.AsEnumerable().Reverse().ToList();

        foreach (var route in _currentRoutes)
        {
            int lcsLength = LongestCommonSubsequence(reversedSolution, route);
            int normalizingLength = Math.Max(1, Math.Min(reversedSolution.Count, route.Count));
            totalRate += (double)lcsLength / normalizingLength;
        }

        return totalRate;
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

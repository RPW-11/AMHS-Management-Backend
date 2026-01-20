using Domain.Mission.ValueObjects;
using static Domain.Mission.ValueObjects.RgvMap;

namespace Infrastructure.RoutePlanning.Rgv;

public class GeneticAlgorithmSolver
{
    private const int PopulationSize = 200;
    private const int MaxGenerations = 200;
    private const double MutationRate = 0.05;
    private const double CrossoverRate = 0.7;
    private const int ChromosomeLength = 1000;
    private const double DuplicateRoutePenaltyRate = 0.5;
    private const double TurnPenaltyRate = 1;

    private readonly Random _random;
    private readonly RgvMap _rgvMap;

    public GeneticAlgorithmSolver(RgvMap rgvMap)
    {
        _random = new Random();
        _rgvMap = rgvMap;
    }

    public List<PathPoint> Solve(List<List<PathPoint>> sampleSolutions)
    {
        var solutions = ModifiedAStar.GetValidSolutions(_rgvMap);
        var population = Enumerable.Range(0, PopulationSize)
                        .Select(_ => GenerateIndividual())
                        .ToList();
        population.AddRange(solutions);
        population.AddRange(sampleSolutions);

        for (int i = 0; i < MaxGenerations; i++)
        {
            var evaluated = population.Select(ind => new
            {
                Individual = ind,
                Fitness = EvaluateFitness(ind),
                Path = ind
            })
            .OrderByDescending(x => x.Fitness)
            .ToList();

            var bestSolution = evaluated.First();

            Console.WriteLine($"Generation {i + 1}: Best Fitness = {bestSolution.Fitness}");

            List<List<PathPoint>> newPopulation = GenerateNewPopulationFromParents(
                [.. evaluated.Select(x => x.Individual)]
            );

            population = newPopulation;
        }

        var bestIndividual = population.Select(ind => new
        {
            Individual = ind,
            Fitness = EvaluateFitness(ind),
            Path = ind
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

            child = Mutate(child);

            newPopulation.Add(child);
        }

        return newPopulation;
    }

    private List<PathPoint> Mutate(List<PathPoint> child)
    {
        if (_random.NextDouble() > MutationRate)
        {
            return child;
        }

        int startIdx = (int)_random.NextInt64(0, Math.Max(0, child.Count-10));
        int endIdx = (int) _random.NextInt64(Math.Min(child.Count-1, startIdx + 5), child.Count-1);

        var startPoint = child[startIdx];
        var endPoint = child[endIdx];

        // Re-compute alternative path between these points
        var subPaths = ModifiedAStar.Solve(_rgvMap, startPoint, endPoint, [], maxSolutionsPerConfig: 1);
        if (subPaths is null || subPaths.Count == 0)
        {
            return child;
        }

        return [.. child.Take(startIdx), .. subPaths[0], .. child.Skip(endIdx+1)];
    }

    private List<PathPoint> TournamentSelection(List<List<PathPoint>> population)
    {
        return population.OrderBy(x => _random.Next())
        .Take(5)
        .OrderByDescending(EvaluateFitness)
        .First();
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
            return 0;
        }

        int numOfDuplicates = CountDuplicates(solution);
        int numOfTurns = CountPathTurns(solution);

        // Compute the optimality value based on throughput, track length, and num of rgvs
        return RouteEvaluator.EvaluateOptimality(solution, _rgvMap) - DuplicateRoutePenaltyRate * numOfDuplicates - TurnPenaltyRate * numOfTurns;
    }

    private bool IsOrderCorrect(List<PathPoint> solution)
    {
        int startIdx = 0;
        bool valid = false;
        foreach (var point in solution)
        {
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

        if (valid && _rgvMap.StationsOrder[0] != solution.Last())
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
}

using Domain.Missions.ValueObjects;
using static Domain.Missions.ValueObjects.Grid;

namespace Infrastructure.RoutePlanning.Rgv;

public class GeneticAlgorithmSolver
{
    private const int PopulationSize = 1000;
    private const double MutationRate = 0.05;
    private const double CrossoverRate = 0.7;
    private const int ChromosomeLength = 1000;
    private const double DuplicateRoutePenaltyRate = 1600;
    private const double TurnPenaltyRate = 2000;
    private const double ConflictPenaltyRate = 4000;
    private const double ForeignStationPenalty = 25;
    private const int EarlyStoppingPatience = 50;
    private const double ElitismRate = 0.1;
    private const int TournamentSize = 5;
    private const int MutationStartIndexMargin = 10;
    private const int MutationMinSegmentLength = 5;
    private const double InvalidSolutionFitness = int.MinValue;

    private readonly Random _random;
    private readonly Grid _grid;
    private readonly List<PathPoint> _stationsOrder;
    private readonly List<SolvedRoute> _solvedRoutes;
    private readonly int _generationsNumber;
    private readonly PathPoint _goalStation;
    private readonly int _goalCount;
    private readonly bool _isClosedLoop;
    private readonly HashSet<Cell> _foreignStations;
    private readonly RouteEvaluator.RouteMetrics _routeMetrics;
    private readonly RouteFitnessWeights _fitnessWeights;

    public GeneticAlgorithmSolver(Grid grid, List<PathPoint> stationsOrder, List<List<PathPoint>> currentRoutes, int generationsNumber, RouteFitnessWeights fitnessWeights)
    {
        _random = new Random();
        _grid = grid;
        _stationsOrder = stationsOrder;
        _solvedRoutes = [.. currentRoutes.Select(ToSolvedRoute)];
        _generationsNumber = generationsNumber;
        _goalStation = stationsOrder[^1];
        _goalCount = stationsOrder.Count(point => point == _goalStation);
        _isClosedLoop = stationsOrder[0] == _goalStation;
        _foreignStations = GetForeignStations(grid, stationsOrder);
        _routeMetrics = RouteEvaluator.GetRouteMetrics(grid, stationsOrder);
        _fitnessWeights = fitnessWeights;
    }

    private sealed class Individual(List<PathPoint> path, double fitness)
    {
        public List<PathPoint> Path { get; } = path;
        public double Fitness { get; } = fitness;
    }

    private Individual CreateIndividual(List<PathPoint> path) => new(path, EvaluateFitness(path));

    public List<PathPoint> Solve()
    {
        Console.WriteLine($"[GA] Seeding A* and RRT");
        List<Individual> population = [
            .. ModifiedAStar.GetValidSolutions(_grid, _stationsOrder).Select(CreateIndividual),
            .. RandomTreeStar.GenerateRRTSolutions(_grid, _stationsOrder).Select(CreateIndividual)
        ];

        while (population.Count < PopulationSize)
        {
            population.Add(CreateIndividual(GenerateRandomWalkPath()));
        }

        double bestFitnessSoFar = double.MinValue;
        int generationsSinceImprovement = 0;

        for (int i = 0; i < _generationsNumber; i++)
        {
            population.Sort((left, right) => right.Fitness.CompareTo(left.Fitness));

            Console.WriteLine($"[GA] Generation {i}: best solution count = {population[0].Path.Count}, fitness = {population[0].Fitness}");

            if (population[0].Fitness > bestFitnessSoFar)
            {
                bestFitnessSoFar = population[0].Fitness;
                generationsSinceImprovement = 0;
            }
            else if (++generationsSinceImprovement >= EarlyStoppingPatience)
            {
                Console.WriteLine($"[GA] Early stopping at generation {i}: no improvement for {EarlyStoppingPatience} generations");
                break;
            }

            population = GenerateNewPopulationFromParents(population);
        }

        var bestIndividual = population.MaxBy(individual => individual.Fitness)!;

        Console.WriteLine($"[GA] Best solution: count = {bestIndividual.Path.Count}, fitness = {bestIndividual.Fitness}");

        if (bestIndividual.Fitness <= InvalidSolutionFitness)
        {
            throw new InvalidOperationException(
                $"Genetic algorithm found no valid route across {_stationsOrder.Count} stations in {_generationsNumber} generations");
        }

        return bestIndividual.Path;
    }

    private List<Individual> GenerateNewPopulationFromParents(List<Individual> sortedParents)
    {
        List<Individual> newPopulation = [];

        newPopulation.AddRange(sortedParents.Take((int)(PopulationSize * ElitismRate)));

        while (newPopulation.Count < PopulationSize)
        {
            List<PathPoint> parent1 = TournamentSelection(sortedParents).Path;
            List<PathPoint> parent2 = TournamentSelection(sortedParents).Path;

            List<PathPoint> child;

            if (_random.NextDouble() < CrossoverRate)
            {
                child = CrossOver(parent1, parent2);
            }
            else
            {
                child = _random.NextDouble() < 0.5 ? parent1 : parent2;
            }

            if (_random.NextDouble() < MutationRate)
            {
                child = Mutate(child);
            }

            newPopulation.Add(CreateIndividual(child));
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

    private Individual TournamentSelection(List<Individual> population)
    {
        var best = population[_random.Next(population.Count)];

        for (int i = 1; i < TournamentSize; i++)
        {
            var challenger = population[_random.Next(population.Count)];

            if (challenger.Fitness > best.Fitness)
            {
                best = challenger;
            }
        }

        return best;
    }

    private List<PathPoint> GenerateRandomWalkPath()
    {
        List<PathPoint> route = [_stationsOrder[0]];

        for (int targetIdx = 1; targetIdx < _stationsOrder.Count; targetIdx++)
        {
            var goal = _stationsOrder[targetIdx];

            var visited = new HashSet<PathPoint> { route[^1] };

            while (route.Count < ChromosomeLength && route[^1] != goal)
            {
                var neighbors = GetValidNeighbors(route[^1])
                    .Where(neighbor => neighbor is not Obstacle && !visited.Contains(neighbor))
                    .ToList();

                if (neighbors.Count == 0)
                {
                    break;
                }

                var next = neighbors[_random.Next(neighbors.Count)];
                route.Add(next);
                visited.Add(next);
            }

            if (route[^1] != goal)
            {
                break;
            }
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
        if (!IsPathConnected(solution)
            || IsPathUsingObstacles(solution)
            || !IsOrderCorrect(solution))
        {
            return InvalidSolutionFitness;
        }

        int length = Math.Max(1, solution.Count);
        double duplicateRate = (double)CountDuplicates(solution) / length;
        double turnRate = (double)CountPathTurns(solution) / length;
        int foreignStationVisits = CountForeignStationVisits(solution);
        var (conflictRate, alignmentRate) = EvaluateRouteOverlap(solution);

        return RouteEvaluator.GetSolutionScores(solution, _routeMetrics, _fitnessWeights).optimality
            - DuplicateRoutePenaltyRate * duplicateRate
            - TurnPenaltyRate * turnRate
            - ConflictPenaltyRate * conflictRate
            - ForeignStationPenalty * foreignStationVisits
            + _fitnessWeights.AlignmentRewardRate * alignmentRate;
    }

    private bool IsOrderCorrect(List<PathPoint> solution)
    {
        int startIdx = 0;
        bool valid = false;
        int goalVisitedCount = 0;

        foreach (var point in solution)
        {
            if (point == _goalStation)
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

        if (valid && _goalStation != solution[^1])
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

    private static HashSet<Cell> GetForeignStations(Grid grid, List<PathPoint> stationsOrder)
    {
        HashSet<Cell> segmentStations = [.. stationsOrder.Select(point => new Cell(point.RowPos, point.ColPos))];
        HashSet<Cell> foreignStations = [];

        for (int row = 0; row < grid.RowDim; row++)
        {
            for (int col = 0; col < grid.ColDim; col++)
            {
                var cell = new Cell(row, col);

                if (grid.MapMatrix[row, col] is Station && !segmentStations.Contains(cell))
                {
                    foreignStations.Add(cell);
                }
            }
        }

        return foreignStations;
    }

    private int CountForeignStationVisits(List<PathPoint> solution)
    {
        if (_foreignStations.Count == 0)
        {
            return 0;
        }

        int visits = 0;

        foreach (var point in solution)
        {
            if (point is Station && _foreignStations.Contains(new Cell(point.RowPos, point.ColPos)))
            {
                visits++;
            }
        }

        return visits;
    }

    private int CountDuplicates(List<PathPoint> solution)
    {
        HashSet<Cell> distinctCells = [];

        foreach (var point in solution)
        {
            distinctCells.Add(new Cell(point.RowPos, point.ColPos));
        }

        int expectedRevisits = _isClosedLoop ? 1 : 0;

        return Math.Max(0, solution.Count - expectedRevisits - distinctCells.Count);
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

    private (double conflictRate, double alignmentRate) EvaluateRouteOverlap(List<PathPoint> solution)
    {
        double conflictTotal = 0;
        double alignmentTotal = 0;

        foreach (var route in _solvedRoutes)
        {
            int normalizingLength = Math.Max(1, Math.Min(solution.Count, route.Length));
            int conflicts = 0;
            int alignments = 0;

            for (int i = 1; i < solution.Count; i++)
            {
                var step = ToStep(solution[i - 1], solution[i]);

                if (route.Steps.Contains(step.Reversed))
                {
                    conflicts++;
                }
                else if (route.Steps.Contains(step))
                {
                    alignments++;
                }
            }

            conflictTotal += (double)conflicts / normalizingLength;
            alignmentTotal += (double)alignments / normalizingLength;
        }

        return (conflictTotal, alignmentTotal);
    }

    private static Step ToStep(PathPoint from, PathPoint to) =>
        new(from.RowPos, from.ColPos, to.RowPos, to.ColPos);

    private static SolvedRoute ToSolvedRoute(List<PathPoint> route)
    {
        HashSet<Step> steps = [];

        for (int i = 1; i < route.Count; i++)
        {
            steps.Add(ToStep(route[i - 1], route[i]));
        }

        return new SolvedRoute(steps, route.Count);
    }

    private readonly record struct Cell(int Row, int Col);

    private readonly record struct Step(int FromRow, int FromCol, int ToRow, int ToCol)
    {
        public Step Reversed => new(ToRow, ToCol, FromRow, FromCol);
    }

    private sealed record SolvedRoute(HashSet<Step> Steps, int Length);
}

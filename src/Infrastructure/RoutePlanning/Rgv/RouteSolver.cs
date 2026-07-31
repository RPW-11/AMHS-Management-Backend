using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.RoutePlanning;
using Domain.Missions.ValueObjects;

namespace Infrastructure.RoutePlanning.Rgv;

public class RouteSolver : IRouteSolver
{
    private const int MaxGenerationsNumber = 400;

    public IEnumerable<PathPoint> Solve(
        Grid grid,
        List<PathPoint> stationsOrder,
        List<List<PathPoint>> currentRoutePoints,
        RoutePlanningAlgorithm routePlanningAlgorithm,
        int generationsNumber,
        RouteSolvePurpose purpose
    )
    {
        if (generationsNumber <= 0 || generationsNumber > MaxGenerationsNumber)
        {
            throw new ArgumentOutOfRangeException(
                nameof(generationsNumber),
                generationsNumber,
                $"Generations number must be between 1 and {MaxGenerationsNumber}");
        }

        if (routePlanningAlgorithm == RoutePlanningAlgorithm.ReinforcementLearning)
        {
            throw new NotImplementedException("Reinforcement learning route planning is not implemented yet");
        }

        var gaSolver = new GeneticAlgorithmSolver(grid, stationsOrder, currentRoutePoints, generationsNumber, RouteFitnessWeights.For(purpose));
        return gaSolver.Solve();
    }

    public RoutePlanningScoreDto GetRouteScore(List<PathPoint> solution, Grid grid, List<PathPoint> stationsOrder, RouteSolvePurpose purpose)
    {
        var (throughput, trackLength, numOfRgvs, optimality) = RouteEvaluator.GetSolutionScores(solution, grid, stationsOrder, RouteFitnessWeights.For(purpose));

        return new(throughput, trackLength, numOfRgvs, optimality);
    }
}

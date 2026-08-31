using Application.Common.Interfaces.RoutePlanning;
using Domain.Missions.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Infrastructure.RoutePlanning.Solvers;

public sealed class GeneticAlgorithmStrategy(ILogger<GeneticAlgorithmSolver> solverLogger) : IPathfindingStrategy
{
    private const int ClusterLoopGenerations = 100;
    private const int ConnectorGenerations = 300;

    private readonly ILogger<GeneticAlgorithmSolver> _solverLogger = solverLogger;

    public RoutePlanningAlgorithm Algorithm => RoutePlanningAlgorithm.GeneticAlgorithm;

    public IReadOnlyList<List<PathPoint>> Solve(RouteSolveRequest request)
    {
        var solver = new GeneticAlgorithmSolver(
            request.Grid,
            request.StationsOrder,
            request.CurrentRoutes,
            GenerationsFor(request.Purpose),
            RouteFitnessWeights.For(request.Purpose),
            request.SeedPaths ?? [],
            _solverLogger);

        return solver.Solve(request.DesiredSolutions);
    }

    private static int GenerationsFor(RouteSolvePurpose purpose) =>
        purpose switch
        {
            RouteSolvePurpose.Connector => ConnectorGenerations,
            _ => ClusterLoopGenerations
        };
}

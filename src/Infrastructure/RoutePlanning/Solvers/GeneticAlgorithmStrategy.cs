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

    public IEnumerable<PathPoint> Solve(
        Grid grid,
        List<PathPoint> stationsOrder,
        List<List<PathPoint>> currentRoutePoints,
        RouteSolvePurpose purpose)
    {
        var solver = new GeneticAlgorithmSolver(
            grid,
            stationsOrder,
            currentRoutePoints,
            GenerationsFor(purpose),
            RouteFitnessWeights.For(purpose),
            _solverLogger);

        return solver.Solve();
    }

    private static int GenerationsFor(RouteSolvePurpose purpose) =>
        purpose switch
        {
            RouteSolvePurpose.Connector => ConnectorGenerations,
            _ => ClusterLoopGenerations
        };
}

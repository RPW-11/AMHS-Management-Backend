using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.RoutePlanning;
using Domain.Missions.ValueObjects;

namespace Infrastructure.RoutePlanning.Solvers;

public class RouteScorer : IRouteScorer
{
    public RoutePlanningScoreDto GetRouteScore(List<PathPoint> solution, Grid grid, IReadOnlyList<IReadOnlyList<Station>> clusterStations, RouteSolvePurpose purpose)
    {
        return RouteEvaluator.GetSolutionScores(solution, grid, clusterStations, RouteFitnessWeights.For(purpose));
    }
}

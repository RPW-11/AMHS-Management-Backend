using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.RoutePlanning;
using Domain.Missions.ValueObjects;

namespace Infrastructure.RoutePlanning.Rgv;

public class RouteScorer : IRouteScorer
{
    public RoutePlanningScoreDto GetRouteScore(List<PathPoint> solution, Grid grid, List<PathPoint> stationsOrder, RouteSolvePurpose purpose)
    {
        var (throughput, trackLength, numOfRgvs, optimality) =
            RouteEvaluator.GetSolutionScores(solution, grid, stationsOrder, RouteFitnessWeights.For(purpose));

        return new(throughput, trackLength, numOfRgvs, optimality);
    }
}

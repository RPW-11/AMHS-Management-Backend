using Application.DTOs.RoutePlanning;
using Domain.Missions.ValueObjects;

namespace Application.Common.Interfaces.RoutePlanning;

public interface IRouteScorer
{
    RoutePlanningScoreDto GetRouteScore(List<PathPoint> solution, Grid grid, IReadOnlyList<IReadOnlyList<Station>> clusterStations, RouteSolvePurpose purpose);
}

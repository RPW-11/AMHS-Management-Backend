using Application.DTOs.RoutePlanning;
using Domain.Missions;
using Domain.Missions.ValueObjects;

namespace Application.Services.RoutePlanningService;

public interface IRouteResultPersister
{
    void Persist(
        MissionBase mission,
        Grid grid,
        RoutePlanningAlgorithm algorithm,
        MemoryStream imageStream,
        List<(List<PathPoint> Solution, string ArrowColor)> routes,
        List<PathPoint> intersections,
        List<RouteSolutionDto> routeSolutions);
}

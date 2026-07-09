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
        byte[] imageBytes,
        List<(List<PathPoint> Solution, string ArrowColor)> routes,
        RgvMapDetailDto rgvMap,
        RoutePlanningScoreDto score);
}

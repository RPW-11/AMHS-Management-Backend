using Application.DTOs.RoutePlanning;
using Domain.Missions.ValueObjects;

namespace Application.Common.Interfaces.RoutePlanning;

public interface IRgvRoutePlanning
{
    IEnumerable<PathPoint> Solve(
        Grid grid,
        List<PathPoint> stationsOrder,
        List<List<PathPoint>> currentRoutePoints,
        RoutePlanningAlgorithm routePlanningAlgorithm,
        int generationsNumber
    );

    byte[] DrawMultipleFlows(
        byte[] imageBytes,
        Grid grid,
        List<(List<PathPoint> Solution, string ArrowColor)> routes);
    string WriteImage(byte[] imageBytes, string fileName);
    void SaveRoutePlanningDetail(RoutePlanningDetailDto routePlanningDetail);
    RoutePlanningSummaryDto GetRoutePlanningSummary(string missionId);
    RoutePlanningScoreDto GetRouteScore(List<PathPoint> solution, Grid grid, List<PathPoint> stationsOrder);
}

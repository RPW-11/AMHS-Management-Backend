using Application.DTOs.RoutePlanning;
using Domain.Missions;
using Domain.Missions.ValueObjects;

namespace Application.Common.Interfaces.RoutePlanning;

public interface IRgvRoutePlanning
{
    (IEnumerable<PathPoint>, IEnumerable<PathPoint>) Solve(RgvMap rgvMap,
                                 List<List<PathPoint>> currentRoutePoints,
                                 RoutePlanningAlgorithm routePlanningAlgorithm);
    string DrawImage(
        byte[] imageBytes,
        string hexColor, 
        RgvMap rgvMap, 
        string name);
    string DrawMultipleFlows(
        byte[] imageBytes,
        List<string> colors,
        RoutePlanningMission routePlanningMission,
        List<PathPoint> intersections,
        string suffix = "");
    string WriteToJson(RoutePlanningDetailDto routePlanningMission);
    RoutePlanningSummaryDto ReadFromJson(string jsonFileUrl);
    RoutePlanningScoreDto GetRouteScore(List<PathPoint> solution, RgvMap rgvMap);
}

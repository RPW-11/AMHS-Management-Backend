using Application.DTOs.Mission.RoutePlanning;
using Domain.Missions;
using Domain.Missions.ValueObjects;

namespace Application.Common.Interfaces.RoutePlanning;

public interface IRgvRoutePlanning
{
    (IEnumerable<PathPoint>, IEnumerable<PathPoint>) Solve(RgvMap rgvMap,
                                 RoutePlanningAlgorithm routePlanningAlgorithm,
                                 List<List<PathPoint>> sampleSolutions);
    string DrawImage(
        byte[] imageBytes,
        string hexColor, 
        RgvMap rgvMap, 
        string name);
    string DrawMultipleFlows(
        byte[] imageBytes,
        List<string> colors,
        RoutePlanningMission routePlanningMission,
        string suffix = "");
    string WriteToJson(RoutePlanningDetailDto routePlanningMission);
    RoutePlanningSummaryDto ReadFromJson(string jsonFileUrl);
    RoutePlanningScoreDto GetRouteScore(List<PathPoint> solution, RgvMap rgvMap);
}

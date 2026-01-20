using Application.DTOs.Mission.RoutePlanning;
using Domain.Mission.ValueObjects;

namespace Application.Common.Interfaces.RoutePlanning;

public interface IRgvRoutePlanning
{
    (IEnumerable<PathPoint>, IEnumerable<PathPoint>) Solve(RoutePlanningDetailDto routePlanningMission,
                                 RoutePlanningAlgorithm routePlanningAlgorithm,
                                 List<List<PathPoint>> sampleSolutions);
    string DrawImage(byte[] imageBytes, RoutePlanningDetailDto routePlanningMission, string suffix = "");
    string WriteToJson(RoutePlanningDetailDto routePlanningMission);
    RoutePlanningSummaryDto ReadFromJson(string jsonFileUrl);
    RoutePlanningScoreDto GetRouteScore(List<PathPoint> solution, RgvMap rgvMap);
}

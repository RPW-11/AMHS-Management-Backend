using Application.DTOs.RoutePlanning;
using Domain.Missions.ValueObjects;

namespace Application.Common.Interfaces.RoutePlanning;

public interface IRgvRoutePlanning
{
    (IEnumerable<PathPoint>, IEnumerable<PathPoint>) Solve(
        RgvMap rgvMap,
        List<List<PathPoint>> currentRoutePoints,
        RoutePlanningAlgorithm routePlanningAlgorithm
    );

    byte[] DrawMultipleFlows(
        byte[] imageBytes,
        List<RgvMap> mapsWithSolutions,
        List<PathPoint> intersections);
    string WriteImage(byte[] imageBytes, string fileName);
    string WriteToJson(RoutePlanningDetailDto routePlanningMission);
    RoutePlanningSummaryDto ReadFromJson(string jsonFileUrl);
    RoutePlanningScoreDto GetRouteScore(List<PathPoint> solution, RgvMap rgvMap);
}

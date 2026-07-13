using Application.DTOs.RoutePlanning;
using Domain.Missions.ValueObjects;

namespace Application.Common.Interfaces.RoutePlanning;

public interface IRoutePlanningResultStore
{
    byte[] DrawMultipleFlows(
        byte[] imageBytes,
        Grid grid,
        List<(List<PathPoint> Solution, string ArrowColor)> routes);

    string WriteImage(byte[] imageBytes, string fileName);

    string GetResultImageUrl(string missionId);

    void SaveRoutePlanningDetail(RoutePlanningDetailDto routePlanningDetail);

    RoutePlanningSummaryDto GetRoutePlanningSummary(string missionId);
}

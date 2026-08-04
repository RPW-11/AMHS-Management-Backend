using Application.DTOs.RoutePlanning;
using Domain.Missions.ValueObjects;

namespace Application.Common.Interfaces.RoutePlanning;

public interface IRoutePlanningResultStore
{
    byte[] DrawMultipleFlows(
        byte[] imageBytes,
        Grid grid,
        List<(List<PathPoint> Solution, string ArrowColor)> routes);
    Task<string> WriteImageAsync(byte[] imageBytes, string missionId, RouteImageKind kind, CancellationToken cancellationToken = default);
    string GetResultImageUrl(string missionId);
    string GetResultJsonUrl(string missionId);
    Task SaveRoutePlanningDetailAsync(RoutePlanningDetailDto routePlanningDetail, CancellationToken cancellationToken = default);
    Task<RoutePlanningSummaryDto> GetRoutePlanningSummaryAsync(string missionId, CancellationToken cancellationToken = default);
}

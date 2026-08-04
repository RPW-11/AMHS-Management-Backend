using Application.DTOs.RoutePlanning;

namespace Application.Common.Interfaces.RoutePlanning;

public interface IRoutePlanningResultStore
{
    Task<string> WriteImageAsync(byte[] imageBytes, string missionId, RouteImageKind kind, CancellationToken cancellationToken = default);
    string GetResultImageUrl(string missionId);
    string GetResultJsonUrl(string missionId);
    Task SaveRoutePlanningDetailAsync(RoutePlanningDetailDto routePlanningDetail, CancellationToken cancellationToken = default);
    Task<RoutePlanningSummaryDto> GetRoutePlanningSummaryAsync(string missionId, CancellationToken cancellationToken = default);
}

using Application.DTOs.RoutePlanning;

namespace Application.Common.Interfaces.RoutePlanning;

public interface IRoutePlanningResultStore
{
    Task<string> WriteImageAsync(byte[] imageBytes, string missionId, RouteImageKind kind, int solutionNumber = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// A download URL for the image of one routing, 1-based and defaulting to the best.
    /// </summary>
    string GetResultImageUrl(string missionId, int solutionNumber = 1);
    string GetResultJsonUrl(string missionId);
    Task SaveRoutePlanningDetailAsync(RoutePlanningDetailDto routePlanningDetail, CancellationToken cancellationToken = default);
    Task<RoutePlanningSummaryDto> GetRoutePlanningSummaryAsync(string missionId, CancellationToken cancellationToken = default);
}

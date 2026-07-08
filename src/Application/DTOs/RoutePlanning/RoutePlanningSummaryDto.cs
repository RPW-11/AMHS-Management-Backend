namespace Application.DTOs.RoutePlanning;

public record RoutePlanningSummaryDto(
    string Algorithm,
    IEnumerable<string> ImageUrls,
    RouteSolutionSummaryDto RouteSolution
);
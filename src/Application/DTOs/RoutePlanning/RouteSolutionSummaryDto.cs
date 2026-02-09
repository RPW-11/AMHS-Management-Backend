namespace Application.DTOs.RoutePlanning;

public record RouteSolutionSummaryDto(
    RgvMapSummaryDto RgvMap,
    RoutePlanningScoreDto Score
);
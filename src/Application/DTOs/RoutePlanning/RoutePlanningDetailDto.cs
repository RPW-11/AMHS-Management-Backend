namespace Application.DTOs.RoutePlanning;

public record RoutePlanningDetailDto(
    string Id,
    string Algorithm,
    IEnumerable<string> ImageUrls,
    RgvMapDetailDto RgvMap,
    RoutePlanningScoreDto Score
);
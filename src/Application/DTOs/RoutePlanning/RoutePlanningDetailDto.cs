namespace Application.DTOs.RoutePlanning;

public record RoutePlanningDetailDto(
    string Id,
    string Algorithm,
    IEnumerable<string> ImageUrls,
    RgvMapDetailDto RgvMap,
    IEnumerable<ClusterFlowSolutionDto> Routes,
    RoutePlanningScoreDto Score
);
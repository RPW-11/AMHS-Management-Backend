namespace Application.DTOs.RoutePlanning;

public record RoutePlanningDetailDto(
    string Id,
    string Algorithm,
    string InputImageUrl,
    IEnumerable<string> ImageUrls,
    RgvMapDetailDto RgvMap,
    IEnumerable<ClusterDefinitionDto> Clusters,
    IEnumerable<ClusterFlowDefinitionDto> ClusterFlows,
    IEnumerable<ClusterFlowSolutionDto> Routes,
    RoutePlanningScoreDto Score
);
namespace Application.DTOs.RoutePlanning;

/// <summary>
/// One complete routing of the map: every flow's clusters and connectors, the image it was drawn
/// onto, and the score of the whole thing.
/// </summary>
/// <param name="ImageUrl">The solved image. Stored as a raw object key; signed on read.</param>
public record RoutePlanningSolutionDto(
    string ImageUrl,
    IEnumerable<ClusterFlowSolutionDto> Routes,
    RoutePlanningScoreDto Score
);

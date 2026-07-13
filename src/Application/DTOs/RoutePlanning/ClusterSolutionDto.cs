namespace Application.DTOs.RoutePlanning;

public record ClusterSolutionDto(string Name, string PathColor, List<PathPointDto> Solution);

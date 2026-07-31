namespace Application.DTOs.RoutePlanning;

public record ClusterDefinitionDto(string Name, string PathColor, List<PathPointDto> Stations);

namespace Application.DTOs.RoutePlanning;

public record ClusterDto(string Name, IEnumerable<PointPositionDto> Stations, string ArrowColor);

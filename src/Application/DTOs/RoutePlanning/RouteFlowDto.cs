namespace Application.DTOs.RoutePlanning;

public record RouteFlowDto(string ArrowColor, IEnumerable<PointPositionDto> StationsOrder);

namespace Application.DTOs.RoutePlanning;

public record PathPointDto(string Name, string Category, PointPositionDto Position, double Time);

namespace API.Contracts.Task;

public record CreateRoutePlanningRequest (
    IFormFile Image,
    string RouteMetadata
);

public record RouteMetadata(
    int RowDim,
    int ColDim,
    string Algorithm,
    IEnumerable<Position> StationsOrder,
    IEnumerable<Point> Points);

public record Point(
    string Name,
    string Category,
    double Time,
    Position Position);

public record Position(
    int RowPos,
    int ColPos);
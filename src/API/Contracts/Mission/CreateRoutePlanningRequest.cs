namespace API.Contracts.Mission;

public record CreateRoutePlanningRequest (
    IFormFile Image,
    string RouteMetadata
);

public record RouteMetadata(
    int RowDim,
    int ColDim,
    int WidthLength,
    int HeightLength,
    string Algorithm,
    IEnumerable<Position> StationsOrder,
    IEnumerable<IEnumerable<Position>> SampleSolutions,
    IEnumerable<Point> Points);

public record Point(
    string Name,
    string Category,
    double Time,
    Position Position);

public record Position(
    int RowPos,
    int ColPos);
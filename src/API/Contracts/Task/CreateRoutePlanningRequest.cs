namespace API.Contracts.Task;

public record CreateRoutePlanningRequest(
    string Image,
    int RowDim,
    int ColDim,
    string Algorithm,
    IEnumerable<Position> StationsOrder,
    IEnumerable<Point> Points);

public record Point(
    string Name,
    string Type,
    double Time,
    Position Position);

public record Position(
    int RowPos,
    int ColPos);
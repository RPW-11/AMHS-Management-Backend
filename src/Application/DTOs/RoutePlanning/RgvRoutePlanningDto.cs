using Domain.Missions.ValueObjects;

namespace Application.DTOs.RoutePlanning;

public record PathPointDto (string Name, string Category, PointPositionDto Position, double Time);
public record PointPositionDto(int RowPos, int ColPos);
public record RouteFlowDto(string ArrowColor, IEnumerable<PointPositionDto> StationsOrder);
public record RoutePlanningScoreDto(
    double Throughput,
    double TrackLength,
    int NumOfRgvs
);
public record RouteSolutionDto(RgvMap RgvMap, RoutePlanningScoreDto Score);
public record RoutePlanningDetailDto(
    string Id,
    string Algorithm,
    IEnumerable<string> ImageUrls,
    List<RouteSolutionDto> RouteSolutions
);
public record RgvMapSummaryDto(
    int RowDim,
    int ColDim,
    int WidthLength,
    int HeightLength
);
public record RouteSolutionSummaryDto(
    RgvMapSummaryDto RgvMap,
    RoutePlanningScoreDto Score
);

public record RoutePlanningSummaryDto(
    string Algorithm,
    IEnumerable<string> ImageUrls,
    IEnumerable<RouteSolutionSummaryDto> RouteSolutions
);
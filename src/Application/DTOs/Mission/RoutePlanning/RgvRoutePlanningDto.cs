using Domain.Mission.ValueObjects;

namespace Application.DTOs.Mission.RoutePlanning;

public record PathPointDto (string Name, string Category, PointPositionDto Position, double Time);
public record PointPositionDto(int RowPos, int ColPos);

public record RoutePlanningDetailDto(
    string Id,
    string Algorithm,
    IEnumerable<string> ImageUrls,
    RgvMap RgvMap,
    RoutePlanningScoreDto? Score
);

public record RoutePlanningSummaryDto(
    string Algorithm,
    IEnumerable<string> ImageUrls,
    RgvMapSummaryDto RgvMap,
    RoutePlanningScoreDto Score
);

public record RoutePlanningScoreDto(
    double Throughput,
    double TrackLength,
    int NumOfRgvs
);

public record RgvMapSummaryDto(
    int RowDim,
    int ColDim,
    int WidthLength,
    int HeightLength
);
using Domain.Mission.ValueObjects;

namespace Application.DTOs.Mission.RoutePlanning;

public record PathPointDto (string Name, string Category, PointPositionDto Position, double Time);
public record PointPositionDto(int RowPos, int ColPos);

public record RoutePlanningDetailDto(
    string Id,
    string Algorithm,
    string ImageUrl,
    RgvMap RgvMap,
    RoutePlanningScoreDto? Score
);

public record RoutePlanningSummaryDto(
    string Algorithm,
    string ImageUrl,
    RoutePlanningScoreDto Score
);

public record RoutePlanningScoreDto(
    double Throughput,
    double TrackLength,
    int NumOfRgvs
);
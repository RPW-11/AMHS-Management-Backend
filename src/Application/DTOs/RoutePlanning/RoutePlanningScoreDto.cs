namespace Application.DTOs.RoutePlanning;

public record RoutePlanningScoreDto(
    double Throughput,
    double TrackLength,
    int NumOfRgvs
);
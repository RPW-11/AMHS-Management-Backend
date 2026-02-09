namespace Application.DTOs.RoutePlanning;

public record RgvMapSummaryDto(
    int RowDim,
    int ColDim,
    int WidthLength,
    int HeightLength
);
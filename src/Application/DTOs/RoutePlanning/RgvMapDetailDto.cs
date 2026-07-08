namespace Application.DTOs.RoutePlanning;

public record RgvMapDetailDto(
    int RowDim,
    int ColDim,
    int WidthLength,
    int HeightLength,
    List<List<PathPointDto>> MapMatrix
);

namespace Application.DTOs.RoutePlanning;

public record RoutePlanningRequest(
    string MissionId,
    MemoryStream ImageStream,
    string Algorithm,
    int RowDim,
    int ColDim,
    int WidthLength,
    int HeightLength,
    IEnumerable<PathPointDto> Points,
    IEnumerable<ClusterDto> Clusters,
    IEnumerable<ClusterFlowDto> ClusterFlows
);

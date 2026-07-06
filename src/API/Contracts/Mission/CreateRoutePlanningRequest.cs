using Application.DTOs.RoutePlanning;

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
    IEnumerable<ClusterDto> Clusters,
    IEnumerable<ClusterFlowDto> ClusterFlows,
    IEnumerable<IEnumerable<PointPositionDto>> SampleSolutions,
    IEnumerable<PathPointDto> Points
);

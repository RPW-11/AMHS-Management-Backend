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
    IEnumerable<RouteFlowDto> RouteFlows,
    IEnumerable<IEnumerable<PointPositionDto>> SampleSolutions,
    IEnumerable<PathPointDto> Points);

namespace Application.DTOs.RoutePlanning;

public record ClusterFlowSolutionDto(string PathColor, List<ClusterSolutionDto> Clusters, List<List<PathPointDto>> ConnectorSolutions);

namespace Application.DTOs.RoutePlanning;

public record ClusterFlowDto(string ArrowColor, IEnumerable<int> ClusterOrder);

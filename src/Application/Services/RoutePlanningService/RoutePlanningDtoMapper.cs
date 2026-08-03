using Application.DTOs.RoutePlanning;
using Domain.Missions.ValueObjects;

namespace Application.Services.RoutePlanningService;

public static class RoutePlanningDtoMapper
{
    public static RgvMapDetailDto ToRgvMapDetailDto(Grid grid)
    {
        List<List<PathPointDto>> mapMatrix = [];

        for (int row = 0; row < grid.RowDim; row++)
        {
            List<PathPointDto> rowPoints = [];

            for (int col = 0; col < grid.ColDim; col++)
            {
                rowPoints.Add(ToPathPointDto(grid[row, col]));
            }

            mapMatrix.Add(rowPoints);
        }

        return new RgvMapDetailDto(grid.RowDim, grid.ColDim, grid.WidthLength, grid.HeightLength, mapMatrix);
    }

    public static List<ClusterDefinitionDto> ToClusterDefinitionDtos(RgvMap rgvMap) =>
        [.. rgvMap.ClusterFlows
            .SelectMany(clusterFlow => clusterFlow.Clusters)
            .DistinctBy(cluster => cluster.Name)
            .Select(cluster => new ClusterDefinitionDto(
                cluster.Name,
                cluster.PathColor,
                [.. cluster.Stations.Select(ToPathPointDto)]
            ))];

    public static List<ClusterFlowDefinitionDto> ToClusterFlowDefinitionDtos(RgvMap rgvMap) =>
        [.. rgvMap.ClusterFlows.Select(clusterFlow => new ClusterFlowDefinitionDto(
            clusterFlow.PathColor,
            [.. clusterFlow.Clusters.Select(cluster => cluster.Name)]
        ))];
    
    public static List<ClusterFlowSolutionDto> ToClusterFlowSolutionDtos(IEnumerable<ClusterFlow> clusterFlows) =>
        [.. clusterFlows.Select(clusterFlow => new ClusterFlowSolutionDto(
            clusterFlow.PathColor,
            [.. clusterFlow.Clusters.Select(cluster => new ClusterSolutionDto(
                cluster.Name,
                cluster.PathColor,
                [.. cluster.Solution.Select(ToPathPointDto)]
            ))],
            [.. clusterFlow.ConnectorSolutions.Select(connectorSolution => new List<PathPointDto>([.. connectorSolution.Select(ToPathPointDto)]))]
        ))];

    public static PathPointDto ToPathPointDto(PathPoint point) =>
        point switch
        {
            Station station => new PathPointDto(station.Name, station.Category.ToString(), new(station.RowPos, station.ColPos), station.ProcessingTime),
            _ => new PathPointDto("", point.Category.ToString(), new(point.RowPos, point.ColPos), 0)
        };
}

using Domain.Common.Models;
using Domain.Errors.Missions.RoutePlanning;
using FluentResults;

namespace Domain.Missions.ValueObjects;

public sealed class RgvMap : ValueObject
{
    public Grid Grid { get; }
    public List<ClusterFlow> ClusterFlows { get; }

    private RgvMap(Grid grid, List<ClusterFlow> clusterFlows)
    {
        Grid = grid;
        ClusterFlows = clusterFlows;
    }

    public static Result<RgvMap> Create(Grid grid, List<ClusterFlow> clusterFlows)
    {
        var clusterFlowsResult = ValidateClusterFlows(grid, clusterFlows);
        if (clusterFlowsResult.IsFailed)
        {
            return Result.Fail(clusterFlowsResult.Errors);
        }

        var clusterNamesResult = ValidateUniqueClusterNames(clusterFlows);
        if (clusterNamesResult.IsFailed)
        {
            return Result.Fail(clusterNamesResult.Errors);
        }

        return new RgvMap(grid, clusterFlows);
    }

    private static Result ValidateUniqueClusterNames(List<ClusterFlow> clusterFlows)
    {
        Dictionary<string, Cluster> clustersByName = [];

        foreach (var clusterFlow in clusterFlows)
        {
            foreach (var cluster in clusterFlow.Clusters)
            {
                if (!clustersByName.TryGetValue(cluster.Name, out var namedCluster))
                {
                    clustersByName[cluster.Name] = cluster;
                    continue;
                }

                if (!IsSameCluster(namedCluster, cluster))
                {
                    return Result.Fail(new DuplicateClusterNameError(cluster.Name));
                }
            }
        }

        return Result.Ok();
    }

    private static bool IsSameCluster(Cluster left, Cluster right)
    {
        if (left.PathColor != right.PathColor || left.Stations.Count != right.Stations.Count)
        {
            return false;
        }

        for (int i = 0; i < left.Stations.Count; i++)
        {
            if (left.Stations[i] != right.Stations[i])
            {
                return false;
            }
        }

        return true;
    }

    private static Result ValidateClusterFlows(Grid grid, List<ClusterFlow> clusterFlows)
    {
        foreach (var clusterFlow in clusterFlows)
        {
            foreach (var cluster in clusterFlow.Clusters)
            {
                foreach (var station in cluster.Stations)
                {
                    if (station.RowPos < 0 || station.RowPos >= grid.RowDim)
                    {
                        return Result.Fail(new InvalidRowPosValueError(station.RowPos, grid.RowDim));
                    }
                    if (station.ColPos < 0 || station.ColPos >= grid.ColDim)
                    {
                        return Result.Fail(new InvalidColPosValueError(station.ColPos, grid.ColDim));
                    }

                    if (grid.MapMatrix[station.RowPos, station.ColPos] is not Station matrixStation || matrixStation.Name != station.Name)
                    {
                        return Result.Fail(new InvalidStationName(station.Name));
                    }
                }
            }
        }

        return Result.Ok();
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Grid;
        yield return ClusterFlows;
    }
}

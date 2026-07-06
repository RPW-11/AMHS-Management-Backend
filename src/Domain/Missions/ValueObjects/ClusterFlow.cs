using Domain.Common.Models;
using Domain.Errors.Missions.RoutePlanning;
using FluentResults;

namespace Domain.Missions.ValueObjects;

public class ClusterFlow : ValueObject
{
    public string PathColor { get; }
    public IReadOnlyList<Cluster> Clusters { get; }
    public IReadOnlyList<PathPoint> Solutions { get; }

    private ClusterFlow(string pathColor, IReadOnlyList<Cluster> clusters, IReadOnlyList<PathPoint> solutions)
    {
        PathColor = pathColor;
        Clusters = clusters;
        Solutions = solutions;
    }

    public static Result<ClusterFlow> Create(string pathColor, IReadOnlyList<Cluster> clusters, IReadOnlyList<PathPoint> solutions)
    {
        for (int i = 0; i < clusters.Count - 1; i++)
        {
            if (clusters[i].Name == clusters[i + 1].Name)
            {
                return Result.Fail(new AdjacentDuplicateClusterError(clusters[i].Name));
            }
        }

        return Result.Ok(new ClusterFlow(pathColor, clusters, solutions));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return PathColor;
        yield return Clusters;
        yield return Solutions;
    }
}

public class Cluster : ValueObject
{
    public string Name { get; }
    public string PathColor { get; }
    public IReadOnlyList<Station> Stations { get; }
    public IReadOnlyList<PathPoint> Solution { get; }

    private Cluster(string name, string pathColor, IReadOnlyList<Station> stations, IReadOnlyList<PathPoint> solution)
    {
        Name = name;
        PathColor = pathColor;
        Stations = stations;
        Solution = solution;
    }

    public static Result<Cluster> Create(string name, string pathColor, IReadOnlyList<Station> stations, IReadOnlyList<PathPoint> solution)
    {
        return Result.Ok(new Cluster(name, pathColor, stations, solution));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return PathColor;
        yield return Stations;
        yield return Solution;
    }
}

using Domain.Missions.ValueObjects;

namespace Application.Services.RoutePlanningService;

/// <summary>
/// The result of solving every unique cluster's own visiting order. Shared unchanged by every
/// composed solution, because a cluster is one piece of physical track and cannot be routed
/// differently in one solution than in another.
/// </summary>
/// <param name="SolutionsByCluster">Each cluster's solved loop. Clusters revisited within a looping flow, or shared between flows, appear once.</param>
/// <param name="Segments">The same loops as a flat list, forming the conflict context every connector solve starts from.</param>
/// <param name="Stations">Each cluster's stations, grouped, for throughput scoring.</param>
public sealed record ClusterLoopSolutions(
    IReadOnlyDictionary<Cluster, List<PathPoint>> SolutionsByCluster,
    IReadOnlyList<List<PathPoint>> Segments,
    IReadOnlyList<IReadOnlyList<Station>> Stations);

using Application.DTOs.RoutePlanning;
using Domain.Missions.ValueObjects;

namespace Application.Services.RoutePlanningService;

/// <summary>
/// One complete routing of the whole map: every flow's clusters and connectors, scored as a whole.
/// </summary>
/// <param name="ClusterFlows">The solved flows, each carrying its clusters' loops and its own connector solutions.</param>
/// <param name="Routes">Every segment paired with the colour it draws in, in drawing order.</param>
/// <param name="Score">The score of the whole routing, including conflicts between one flow and another.</param>
public sealed record ComposedSolution(
    IReadOnlyList<ClusterFlow> ClusterFlows,
    List<(List<PathPoint> Solution, string ArrowColor)> Routes,
    RoutePlanningScoreDto Score);

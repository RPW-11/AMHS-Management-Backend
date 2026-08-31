using Domain.Missions.ValueObjects;

namespace Application.Common.Interfaces.RoutePlanning;

/// <summary>
/// One route for a pathfinding strategy to solve.
/// </summary>
/// <param name="Grid">The layout being routed over.</param>
/// <param name="StationsOrder">The points to visit, in order. A closed loop repeats its first point last.</param>
/// <param name="CurrentRoutes">Routes already solved, which the solution is scored against for conflicts and alignment.</param>
/// <param name="Purpose">Selects the generation count and fitness weights.</param>
/// <param name="DesiredSolutions">How many distinct candidates to return. Fewer may come back.</param>
/// <param name="SeedPaths">
/// Routes to seed the initial population with, on top of the strategy's own seeding. Used to warm
/// start a solve that differs from an earlier one only in <paramref name="CurrentRoutes"/>, where
/// the earlier answer is already close to a good one.
/// </param>
public sealed record RouteSolveRequest(
    Grid Grid,
    List<PathPoint> StationsOrder,
    List<List<PathPoint>> CurrentRoutes,
    RouteSolvePurpose Purpose,
    int DesiredSolutions = 1,
    IReadOnlyList<List<PathPoint>>? SeedPaths = null);

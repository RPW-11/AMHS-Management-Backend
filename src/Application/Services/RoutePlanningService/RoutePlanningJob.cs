using Domain.Missions.ValueObjects;

namespace Application.Services.RoutePlanningService;

/// <summary>
/// Everything the background solve needs, as plain data. This is the whole capture list of the
/// enqueued closure, so keep it that way: anything resolved from the request's DI scope would
/// outlive that scope.
/// </summary>
public sealed record RoutePlanningJob(
    MissionId MissionId,
    RgvMap RgvMap,
    RoutePlanningAlgorithm Algorithm,
    byte[] ImageBytes);

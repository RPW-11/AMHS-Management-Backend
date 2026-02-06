using Domain.Interfaces;
using Domain.Missions.ValueObjects;

namespace Domain.Missions.Events;

public record MissionRoutePlanningStartedEvent(
    MissionId MissionId,
    IEnumerable<RgvMap> RgvMaps,
    RoutePlanningAlgorithm RoutePlanningAlgorithm,
    MemoryStream ImageStream
) : IDomainEvent;
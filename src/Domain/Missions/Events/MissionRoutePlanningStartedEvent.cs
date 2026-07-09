using Domain.Employees.ValueObjects;
using Domain.Interfaces;
using Domain.Missions.ValueObjects;

namespace Domain.Missions.Events;

public record MissionRoutePlanningStartedEvent(
    MissionId MissionId,
    string MissionName,
    IReadOnlyList<EmployeeId> AssignedEmployeeIds
) : IDomainEvent;

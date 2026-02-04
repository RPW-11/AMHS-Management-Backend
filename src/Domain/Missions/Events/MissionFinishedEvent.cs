using Domain.Employees.ValueObjects;
using Domain.Interfaces;
using Domain.Missions.ValueObjects;

namespace Domain.Missions.Events;

public record MissionFinishedEvent(
    MissionId MissionId,
    string MissionName,
    IReadOnlyList<EmployeeId> AssignedEmployeeIds,
    DateTime FinishedAt
) : IDomainEvent;
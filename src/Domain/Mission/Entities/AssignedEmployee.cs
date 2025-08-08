using Domain.Common.Models;
using Domain.Employee.ValueObjects;
using Domain.Mission.ValueObjects;
using FluentResults;

namespace Domain.Mission.Entities;

public sealed class AssignedEmployee : Entity<AssignedEmployeeId>
{
    public MissionId MissionId { get; }
    public EmployeeId EmployeeId { get; }
    public MissionRole MissionRole { get; private set; }

    private AssignedEmployee(MissionId missionId, EmployeeId employeeId, MissionRole missionRole)
        : base(AssignedEmployeeId.CreateUnique(missionId.ToString(), employeeId.ToString()))
    {
        MissionId = missionId;
        EmployeeId = employeeId;
        MissionRole = missionRole;
    }

    public static Result<AssignedEmployee> Create(MissionId missionId,
                                                  EmployeeId employeeId,
                                                  MissionRole missionRole)
    {
        AssignedEmployeeId id = AssignedEmployeeId.CreateUnique(missionId.ToString(), employeeId.ToString());

        return new AssignedEmployee(missionId, employeeId, missionRole);
    }   
}

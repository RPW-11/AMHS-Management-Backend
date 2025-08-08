using Domain.Common.Models;
using Domain.Employee.ValueObjects;
using Domain.Errors.Mission;
using Domain.Mission.Entities;
using Domain.Mission.ValueObjects;
using FluentResults;

namespace Domain.Mission;

public class MissionBase : AggregateRoot<MissionId>
{
    private const int MaxNumberOfLeader = 1;
    private const int MaxNumberOfCoLeader = 3;
    private readonly List<AssignedEmployee> _assignedEmployees = [];
    public IReadOnlyList<AssignedEmployee> AssignedEmployees => _assignedEmployees.AsReadOnly();
    public string Name { get; private set; }
    public string Description { get; private set; }
    public MissionCategory Category { get; private set; }
    public MissionStatus Status { get; private set; }
    public DateTime FinishedAt { get; private set; }
    public string? ResourceLink { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private MissionBase(MissionId id): base(id){}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    protected MissionBase(MissionId id,
                          string name,
                          string description,
                          MissionCategory category,
                          MissionStatus status,
                          AssignedEmployee leader,
                          DateTime finishedAt)
    : base(id)
    {
        _assignedEmployees.Add(leader);
        Name = name;
        Description = description;
        Category = category;
        Status = status;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        FinishedAt = finishedAt;
    }

    public Result AddEmployee(EmployeeId employeeId, string missionRole)
    {
        var roleResult = MissionRole.FromString(missionRole);
        if (roleResult.IsFailed)
        {
            return Result.Fail(roleResult.Errors[0]);
        }

        // Check invariants
        if (roleResult.Value != MissionRole.Member)
        {
            int roleCount = _assignedEmployees.Sum(ae => ae.MissionRole == roleResult.Value ? 1 : 0) + 1;

            if (roleResult.Value == MissionRole.CoLeader
                && roleCount > MaxNumberOfCoLeader
                || roleResult.Value == MissionRole.Leader
                && roleCount > MaxNumberOfLeader)
            {
                return Result.Fail(new MaximumMissionRoleError());
            }
        }

        var assignedEmployeeResult = AssignedEmployee.Create(Id, employeeId, roleResult.Value);
        _assignedEmployees.Add(assignedEmployeeResult.Value);

        return Result.Ok();
    }
}

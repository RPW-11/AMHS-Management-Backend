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
                          List<AssignedEmployee> assignedEmployees,
                          DateTime finishedAt)
    : base(id)
    {
        _assignedEmployees = assignedEmployees;
        Name = name;
        Description = description;
        Category = category;
        Status = status;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        FinishedAt = finishedAt;
    }

    public static MissionBase Create(MissionId missionId,
                                     string name,
                                     MissionCategory category,
                                     AssignedEmployee leader,
                                     DateTime finishedAt,
                                     string description = "")
    {
        return new(
            missionId,
            name,
            description,
            category,
            MissionStatus.Active,
            [leader],
            finishedAt
        );
    }

    public void SetMissionStatus(MissionStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetMissionCategory(MissionCategory category)
    {
        Category = category;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetMissionName(string name)
    {
        Name = name;
    }

    public void SetMissionDescription(string description)
    {
        Description = description;
    }

    public void SetMissionResourceLink(string resourceLink)
    {
        ResourceLink = resourceLink;
        UpdatedAt = DateTime.UtcNow;
    }

    public Result AddEmployee(EmployeeId employeeId, MissionRole missionRole)
    {
        // Check invariants
        if (missionRole != MissionRole.Member)
        {
            int roleCount = _assignedEmployees.Sum(ae => ae.MissionRole == missionRole ? 1 : 0) + 1;

            if (missionRole == MissionRole.CoLeader
                && roleCount > MaxNumberOfCoLeader
                || missionRole == MissionRole.Leader
                && roleCount > MaxNumberOfLeader)
            {
                return Result.Fail(new MaximumMissionRoleError());
            }
        }

        var assignedEmployeeResult = AssignedEmployee.Create(Id, employeeId, missionRole);
        _assignedEmployees.Add(assignedEmployeeResult.Value);

        UpdatedAt = DateTime.UtcNow;

        return Result.Ok();
    }
}

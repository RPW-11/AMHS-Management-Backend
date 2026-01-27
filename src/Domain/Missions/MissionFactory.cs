using Domain.Employees.ValueObjects;
using Domain.Errors.Missions;
using Domain.Missions.Entities;
using Domain.Missions.ValueObjects;
using FluentResults;

namespace Domain.Missions;

public static class MissionFactory
{
    public static Result<MissionBase> CreateMission(string employeeId,
                                         string name,
                                         string category,
                                         string description,
                                         DateTime finishedAt)
    {
        var missionCategoryResult = MissionCategory.FromString(category);
        if (missionCategoryResult.IsFailed)
        {
            return Result.Fail<MissionBase>(missionCategoryResult.Errors[0]);
        }

        var leaderIdResult = EmployeeId.FromString(employeeId);
        if (leaderIdResult.IsFailed)
        {
            return Result.Fail<MissionBase>(leaderIdResult.Errors[0]);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail<MissionBase>(new EmptyMissionNameError());
        }

        var missionId = MissionId.CreateUnique();

        var assignedLeader = AssignedEmployee.Create(missionId,
                                                     leaderIdResult.Value,
                                                     MissionRole.Leader);

        if (assignedLeader.IsFailed)
        {
            return Result.Fail(assignedLeader.Errors[0]);
        }

        var missionBase = MissionBase.Create(missionId, name, missionCategoryResult.Value, assignedLeader.Value, finishedAt, description);

        return missionBase;
    }
}

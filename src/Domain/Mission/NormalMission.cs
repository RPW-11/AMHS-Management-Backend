using Domain.Mission.Entities;
using Domain.Mission.ValueObjects;
using FluentResults;

namespace Domain.Mission;

public class NormalMission : MissionBase
{
    private NormalMission(MissionId missionId,
                         string name,
                         string description,
                         MissionCategory category,
                         MissionStatus status,
                         AssignedEmployee leader,
                         DateTime finishedAt)
    : base(missionId, name, description, category, status, leader, finishedAt)
    {
    }

    public static Result<NormalMission> Create(MissionId missionId,
                                               string name,
                                               AssignedEmployee leader,
                                               DateTime finishedAt,
                                               string description = "")
    {
        return new NormalMission(
            missionId,
            name,
            description,
            MissionCategory.Normal,
            MissionStatus.Active,
            leader,
            finishedAt
        );
    }
}

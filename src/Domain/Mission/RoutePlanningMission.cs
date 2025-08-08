using Domain.Mission.Entities;
using Domain.Mission.ValueObjects;
using FluentResults;

namespace Domain.Mission;

public class RoutePlanningMission : MissionBase
{
    public RgvMap? RgvMap { get; private set; }

    private RoutePlanningMission(MissionId missionId,
                          string name,
                          string description,
                          MissionCategory category,
                          MissionStatus status,
                          AssignedEmployee leader,
                          DateTime finishedAt)
    : base(missionId,
          name,
          description,
          category,
          status,
          leader,
          finishedAt)
    {

    }
    public static Result<RoutePlanningMission> Create(MissionId missionId,
                                                      string name,
                                                      AssignedEmployee leader,
                                                      DateTime finishedAt,
                                                      string description = "")
    {
        return new RoutePlanningMission(missionId,
                   name,
                   description,
                   MissionCategory.RoutePlanning,
                   MissionStatus.Active,
                   leader,
                   finishedAt);
    }

    public void SetRgvMap(RgvMap newMap)
    {
        RgvMap = newMap;
    }
}

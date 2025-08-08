using Domain.Common.Models;
using Domain.Errors.Mission;
using FluentResults;

namespace Domain.Mission.ValueObjects;

public sealed class MissionRole: ValueObject
{
    private enum Role
    {
        Member,
        Leader,
        CoLeader
    }

    private readonly Role _value;

    private MissionRole(Role value)
    {
        _value = value;
    }

    public static MissionRole Member => new(Role.Member);
    public static MissionRole CoLeader => new(Role.CoLeader);
    public static MissionRole Leader => new(Role.Leader);


    public static Result<MissionRole> FromString(string missionRole)
    {
        if (string.IsNullOrEmpty(missionRole))
        {
            return Result.Fail<MissionRole>(new EmptyMissionRoleError());
        }

        return missionRole.ToLower() switch
        {
            "member" => Member,
            "coleader" => CoLeader,
            "leader" => Leader,
            _ => Result.Fail<MissionRole>(new InvalidMissionRoleError(missionRole))
        };
    }

    public override string ToString() => _value.ToString();
    
    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }
}

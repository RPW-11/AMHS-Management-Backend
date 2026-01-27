using Domain.Common.Models;
using Domain.Errors.Missions;
using FluentResults;

namespace Domain.Missions.ValueObjects;

public sealed class MissionStatus: ValueObject
{
    private enum StatusValue
    {
        Active,
        Inactive,
        Finished,
    }
    private readonly StatusValue _value;

    private MissionStatus(StatusValue value)
    {
        _value = value;
    }

    public static MissionStatus Active => new(StatusValue.Active);
    public static MissionStatus Inactive => new(StatusValue.Inactive);
    public static MissionStatus Finished => new(StatusValue.Finished);

    public static Result<MissionStatus> FromString(string status)
    {
        if (string.IsNullOrEmpty(status))
        {
            return Result.Fail<MissionStatus>(new InvalidMissionStatusError(status));
        }

        return status.ToLower() switch
        {
            "active" => Active,
            "inactive" => Inactive,
            "finished" => Finished,
            _ => Result.Fail<MissionStatus>(new InvalidMissionStatusError(status))
        };
    }

    public override string ToString() => _value.ToString();

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }
}

using Domain.Errors.Mission;
using FluentResults;

namespace Domain.ValueObjects.Mission;

public class MissionStatus
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

    public override bool Equals(object? obj) => 
        obj is MissionStatus other && _value == other._value;

    public override int GetHashCode() => _value.GetHashCode();

    public static bool operator ==(MissionStatus left, MissionStatus right) => 
        left?.Equals(right) ?? right is null;

    public static bool operator !=(MissionStatus left, MissionStatus right) => 
        !(left == right);
}

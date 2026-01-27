using Domain.Common.Models;
using Domain.Errors.Missions;
using FluentResults;

namespace Domain.Missions.ValueObjects;

public sealed class MissionId : ValueObject
{
    public Guid Value { get; }

    private MissionId(Guid id)
    {
        Value = id;
    }

    public static MissionId CreateUnique()
    {
        return new MissionId(Guid.NewGuid());
    }
    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static Result<MissionId> FromString(string value)
    {
        if (Guid.TryParse(value, out Guid id))
        {
            return new MissionId(id);
        }

        return Result.Fail<MissionId>(new InvalidMissionIdError(value));
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

using Domain.Common.Models;
using Domain.Errors.Missions;
using FluentResults;

namespace Domain.Missions.ValueObjects;

public sealed class MissionCategory : ValueObject
{
    private enum CategoryValue
    {
        RoutePlanning,
        Normal
    }

    private readonly CategoryValue _value;

    private MissionCategory(CategoryValue value)
    {
        _value = value;
    }

    public static MissionCategory RoutePlanning => new(CategoryValue.RoutePlanning);
    public static MissionCategory Normal => new(CategoryValue.Normal);

    public static Result<MissionCategory> FromString(string category)
    {
        if (string.IsNullOrEmpty(category))
        {
            return Result.Fail<MissionCategory>(new InvalidMissionCategoryError(category));
        }

        return category.ToLower() switch
        {
            "routeplanning" => RoutePlanning,
            "normal" => Normal,
            _ => Result.Fail<MissionCategory>(new InvalidMissionCategoryError(category))
        };
    }

    public override string ToString() => _value.ToString();
    
    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }
}

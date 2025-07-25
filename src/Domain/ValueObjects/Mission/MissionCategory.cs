using Domain.Errors.Mission;
using FluentResults;

namespace Domain.ValueObjects.Mission;

public class MissionCategory
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

    public override bool Equals(object? obj) => 
        obj is MissionCategory other && _value == other._value;

    public override int GetHashCode() => _value.GetHashCode();

    public static bool operator ==(MissionCategory left, MissionCategory right) => 
        left?.Equals(right) ?? right is null;

    public static bool operator !=(MissionCategory left, MissionCategory right) => 
        !(left == right);

}

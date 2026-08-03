using Domain.Common.Models;

namespace Domain.Missions.ValueObjects;

public class PointCategory : ValueObject
{
    private enum Category
    {
        Station,
        Obstacle,
        Path
    }

    private const string StationCode = "st";
    private const string ObstacleCode = "obs";
    private const string PathCode = "path";

    private readonly Category _value;
    private readonly string _code;

    private PointCategory(Category value, string code)
    {
        _value = value;
        _code = code;
    }

    public static PointCategory Station { get; } = new(Category.Station, StationCode);
    public static PointCategory Obstacle { get; } = new(Category.Obstacle, ObstacleCode);
    public static PointCategory Path { get; } = new(Category.Path, PathCode);

    public static PointCategory FromString(string? category) =>
        category?.ToLower() switch
        {
            ObstacleCode => Obstacle,
            StationCode => Station,
            _ => Path
        };

    public override string ToString() => _code;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }
}

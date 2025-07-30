using FluentResults;

namespace Domain.ValueObjects.Mission.RoutePlanning;

public class PathPoint
{
    public enum PointCategory
    {
        Obstacle,
        Path,
        Station
    }
    public string Name { get; }
    public PointCategory Category { get; }
    public int RowPos { get; }
    public int ColPos { get; }
    public double Time { get; }

    private PathPoint(string name, PointCategory category, int rowPos, int colPos, double time)
    {
        Name = name;
        Category = category;
        RowPos = rowPos;
        ColPos = colPos;
        Time = time;
    }

    public static PathPoint Path(int rowPos, int colPos) => new("Path", PointCategory.Path, rowPos, colPos, 0);

    public static Result<PathPoint> Create(string name, string category, int rowPos, int colPos, double time)
    {
        return new PathPoint(
            name,
            GetPointCategoryFromString(category),
            rowPos,
            colPos,
            time
        );
    }

    private static PointCategory GetPointCategoryFromString(string pointType)
    {
        return pointType.ToLower() switch
        {
            "obs" => PointCategory.Obstacle,
            "st" => PointCategory.Station,
            _ => PointCategory.Path
        };
    }
}
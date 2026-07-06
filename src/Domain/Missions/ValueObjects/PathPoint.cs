using Domain.Common.Models;

namespace Domain.Missions.ValueObjects;



public abstract class PathPoint(int rowPos, int colPos) : ValueObject 
{
    public int RowPos { get; } = rowPos;
    public int ColPos { get; } = colPos;
}

public class Station(int rowPos, int colPos, string name, double processingTime) : PathPoint(rowPos, colPos)
{
    public string Name { get; } = name;
    public double ProcessingTime { get; } = processingTime;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return RowPos;
        yield return ColPos;
        yield return Name;
        yield return ProcessingTime;
    }
}

public class Obstacle(int rowPos, int colPos) : PathPoint(rowPos, colPos)
{
    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return RowPos;
        yield return ColPos;
    }
}

public class Path(int rowPos, int colPos) : PathPoint(rowPos, colPos)
{
    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return RowPos;
        yield return ColPos;
    }
}

public enum PointCategory
{
    Station,
    Obstacle,
    Path
}

public static class PointFactory
{
    public static PathPoint Create(PointCategory pointCategory, int rowPos, int colPos, string? name, double? processingTime)
    {
        return pointCategory switch
        {
            PointCategory.Station => new Station(
                rowPos,
                colPos, 
                name ?? throw new ArgumentException("Station requires a name."),
                processingTime ?? throw new ArgumentException("Station requires a name.")
            ),
            PointCategory.Obstacle => new Obstacle(rowPos, colPos),
            PointCategory.Path => new Path(rowPos, colPos),
            _ => throw new ArgumentException($"Unknown path point category: {pointCategory}")
        };
    }
}

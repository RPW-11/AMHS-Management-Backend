using Domain.Common.Models;
using FluentResults;

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
    public static Result<PathPoint> Create(PointCategory pointCategory, int rowPos, int colPos, string? name, double? processingTime)
    {
        switch (pointCategory)
        {
            case PointCategory.Station:
                if (name is null)
                {
                    return Result.Fail<PathPoint>("Station requires a name.");
                }

                if (processingTime is null)
                {
                    return Result.Fail<PathPoint>("Station requires a processing time.");
                }

                return Result.Ok<PathPoint>(new Station(rowPos, colPos, name, processingTime.Value));

            case PointCategory.Obstacle:
                return Result.Ok<PathPoint>(new Obstacle(rowPos, colPos));

            case PointCategory.Path:
                return Result.Ok<PathPoint>(new Path(rowPos, colPos));

            default:
                return Result.Fail<PathPoint>($"Unknown path point category: {pointCategory}");
        }
    }
}

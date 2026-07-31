using Domain.Common.Models;
using FluentResults;

namespace Domain.Missions.ValueObjects;



/// <remarks>
/// Equality is implemented directly rather than through <see cref="ValueObject"/>'s
/// GetEqualityComponents, which allocates an iterator and boxes every component on each call.
/// Route solving compares points millions of times per run - Contains, Intersect and hash lookups
/// all go through here - so the cost of that is not affordable. The overrides below must stay
/// consistent with the GetEqualityComponents implementations in the derived types.
/// </remarks>
public abstract class PathPoint : ValueObject
{
    private readonly int _hashCode;

    protected PathPoint(int rowPos, int colPos)
    {
        RowPos = rowPos;
        ColPos = colPos;

        // Position and concrete type only: a station's name and processing time are not assigned
        // until its own constructor runs. Equal points still produce equal hashes, which is all a
        // hash code has to guarantee, and no two points can share a cell on the grid anyway.
        _hashCode = HashCode.Combine(GetType(), rowPos, colPos);
    }

    public int RowPos { get; }
    public int ColPos { get; }

    public override int GetHashCode() => _hashCode;

    public override bool Equals(object? obj)
    {
        // The grid holds one instance per cell and every solver point comes from it, so almost
        // every comparison is between the same instance and stops here.
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj is PathPoint other
            && other.GetType() == GetType()
            && RowPos == other.RowPos
            && ColPos == other.ColPos
            && HasSameDetails(other);
    }

    /// <summary>
    /// Compares whatever the concrete type carries beyond its position and type.
    /// </summary>
    protected virtual bool HasSameDetails(PathPoint other) => true;
}

public class Station(int rowPos, int colPos, string name, double processingTime) : PathPoint(rowPos, colPos)
{
    public string Name { get; } = name;
    public double ProcessingTime { get; } = processingTime;

    protected override bool HasSameDetails(PathPoint other) =>
        other is Station station
            && Name == station.Name
            && ProcessingTime.Equals(station.ProcessingTime);

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

using Domain.Errors.Task;
using FluentResults;

namespace Domain.ValueObjects.Task;

public class TaskCategory
{
    private enum CategoryValue
    {
        RoutePlanning,
        Normal
    }

    private readonly CategoryValue _value;

    private TaskCategory(CategoryValue value)
    {
        _value = value;
    }

    public static TaskCategory RoutePlanning => new(CategoryValue.RoutePlanning);
    public static TaskCategory Normal => new(CategoryValue.Normal);

    public static Result<TaskCategory> FromString(string category)
    {
        if (string.IsNullOrEmpty(category))
        {
            return Result.Fail<TaskCategory>(new InvalidTaskCategoryError(category));
        }

        return category.ToLower() switch
        {
            "routeplanning" => RoutePlanning,
            "normal" => Normal,
            _ => Result.Fail<TaskCategory>(new InvalidTaskCategoryError(category))
        };
    }

    public override string ToString() => _value.ToString();

    public override bool Equals(object? obj) => 
        obj is TaskCategory other && _value == other._value;

    public override int GetHashCode() => _value.GetHashCode();

    public static bool operator ==(TaskCategory left, TaskCategory right) => 
        left?.Equals(right) ?? right is null;

    public static bool operator !=(TaskCategory left, TaskCategory right) => 
        !(left == right);

}

using Domain.Errors.Task;
using FluentResults;

namespace Domain.ValueObjects.Task;

public class TaskStatus
{
    private enum StatusValue
    {
        Active,
        Inactive,
        Finished,
    }
    private readonly StatusValue _value;

    private TaskStatus(StatusValue value)
    {
        _value = value;
    }

    public static TaskStatus Active => new(StatusValue.Active);
    public static TaskStatus Inactive => new(StatusValue.Inactive);
    public static TaskStatus Finished => new(StatusValue.Finished);

    public static Result<TaskStatus> FromString(string status)
    {
        if (string.IsNullOrEmpty(status))
        {
            return Result.Fail<TaskStatus>(new InvalidTaskStatusError(status));
        }

        return status.ToLower() switch
        {
            "active" => Active,
            "inactive" => Inactive,
            "finished" => Finished,
            _ => Result.Fail<TaskStatus>(new InvalidTaskStatusError(status))
        };
    }

    public override string ToString() => _value.ToString();

    public override bool Equals(object? obj) => 
        obj is TaskStatus other && _value == other._value;

    public override int GetHashCode() => _value.GetHashCode();

    public static bool operator ==(TaskStatus left, TaskStatus right) => 
        left?.Equals(right) ?? right is null;

    public static bool operator !=(TaskStatus left, TaskStatus right) => 
        !(left == right);
}

using Domain.ValueObjects.Task;
using FluentResults;
using TaskStatus = Domain.ValueObjects.Task.TaskStatus; // conflicting with system threading task

namespace Domain.Entities;

public class Task
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public TaskCategory Category { get; private set; }
    public TaskStatus Status { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    public string? ResourceLink { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Task(Guid id, string name, string description, TaskCategory category, TaskStatus status)
    {
        Id = id;
        Name = name;
        Description = description;
        Category = category;
        Status = status;
    }

    public static Result<Task> Create(Guid id, string name, string category, string status, string description = "")
    {
        var statusResult = TaskStatus.FromString(status);
        var categoryResult = TaskCategory.FromString(category);

        if (statusResult.IsFailed)
        {
            return Result.Fail(statusResult.Errors[0]);
        }
        if (categoryResult.IsFailed)
        {
            return Result.Fail(categoryResult.Errors[0]);
        }


        return new Task(id, name, description, categoryResult.Value, statusResult.Value);
    }
}

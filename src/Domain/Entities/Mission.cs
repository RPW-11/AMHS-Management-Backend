using Domain.ValueObjects.Mission;
using FluentResults;

namespace Domain.Entities;

public class Mission
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public MissionCategory Category { get; private set; }
    public MissionStatus Status { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    public string? ResourceLink { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Mission(Guid id, string name, string description, MissionCategory category, MissionStatus status)
    {
        Id = id;
        Name = name;
        Description = description;
        Category = category;
        Status = status;
        CreatedAt = new DateTime();
        UpdatedAt = new DateTime();
    }

    public static Result<Mission> Create(Guid id, string name, string category, string status, string description = "")
    {
        var statusResult = MissionStatus.FromString(status);
        var categoryResult = MissionCategory.FromString(category);

        if (statusResult.IsFailed)
        {
            return Result.Fail(statusResult.Errors[0]);
        }
        if (categoryResult.IsFailed)
        {
            return Result.Fail(categoryResult.Errors[0]);
        }

        return new Mission(id, name, description, categoryResult.Value, statusResult.Value);
    }
}

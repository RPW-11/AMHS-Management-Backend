using Domain.Common.Models;

namespace Domain.Notifications.ValueObjects;

public sealed class NotificationTarget : ValueObject
{
    public Guid Id { get; private set; }
    public string Type { get; private set; }

    private NotificationTarget(Guid id, string type)
    {
        Id = id;
        Type = type;
    }
    public static NotificationTarget Create(Guid id, string type)
    {
        return new NotificationTarget(id, type);
    }

    public override string ToString() => Type;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return (Id, Type);
    }
}
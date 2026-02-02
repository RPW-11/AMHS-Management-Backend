using Domain.Common.Models;

namespace Domain.Notifications.ValueObjects;

public sealed class NotificationTarget : ValueObject
{
    public int Id { get; private set; }
    public string Type { get; private set; }

    public NotificationTarget(int id, string type)
    {
        Id = id;
        Type = type;
    }

    public override string ToString() => Type;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return (Id, Type);
    }
}
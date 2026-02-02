using Domain.Common.Models;
using Domain.Errors.Notifications;
using FluentResults;

namespace Domain.Notifications.ValueObjects;

public sealed class NotificationId : ValueObject
{
    public Guid Value { get; }

    private NotificationId(Guid id)
    {
        Value = id;
    }

    public static NotificationId CreateUnique()
    {
        return new NotificationId(Guid.NewGuid());
    }
    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static Result<NotificationId> FromString(string value)
    {
        if (Guid.TryParse(value, out Guid id))
        {
            return new NotificationId(id);
        }

        return Result.Fail<NotificationId>(new InvalidNotificationIdError(value));
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
using Domain.Common.Models;
using Domain.Errors.Notifications;
using FluentResults;

namespace Domain.Notifications.ValueObjects;

public sealed class NotificationType : ValueObject
{
    private enum TypeValue
    {
        Add,
        Remove,
        MissionFinished,
        Info
    }
    private readonly TypeValue _value;

    private NotificationType(TypeValue value)
    {
        _value = value;
    }

    public static NotificationType Add => new(TypeValue.Add);
    public static NotificationType Remove => new(TypeValue.Remove);
    public static NotificationType Info => new(TypeValue.Info);
    public static NotificationType MissionFinished => new(TypeValue.MissionFinished);
    public static Result<NotificationType> FromString(string notificationType)
    {
        if (string.IsNullOrEmpty(notificationType))
        {
            return Result.Fail<NotificationType>(new InvalidNotificationType(notificationType));
        }

        return notificationType.ToLower() switch
        {
            "add" => Add,
            "remove" => Remove,
            "info" => Info,
            "missionfinished" => MissionFinished,
            _ => Result.Fail<NotificationType>(new InvalidNotificationType(notificationType))
        };
    }

    public override string ToString() => _value.ToString();

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }
}
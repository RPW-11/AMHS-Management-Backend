using Domain.Common.Models;
using Domain.Employees.ValueObjects;
using Domain.Notification.ValueObjects;
using FluentResults;

namespace Domain.Notification;


public class Notification : AggregateRoot<NotificationId>
{
    public EmployeeId RecipientId { get; private set; }
    public EmployeeId? ActorId { get; private set; }
    public string ActorName { get; private set; }
    public string? ActorAvatarUrl { get; private set; }
    public NotificationTarget NotificationTarget { get; private set; }

    public NotificationType NotificationType { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }

    private Notification(
        NotificationId id,
        EmployeeId recipientId,
        EmployeeId? actorId,
        string actorName,
        string? actorAvatarUrl,
        NotificationTarget notificationTarget,
        NotificationType notificationType
        ) : base(id)
    {
        RecipientId = recipientId;
        ActorId = actorId;
        ActorName = actorName;
        ActorAvatarUrl = actorAvatarUrl;
        NotificationTarget = notificationTarget;
        NotificationType = notificationType;
        CreatedAt = DateTime.UtcNow;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    protected Notification(NotificationId id) : base(id) { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public static Result<Notification> Create(
        EmployeeId recipientId,
        EmployeeId? actorId,
        string actorName,
        string? actorAvatarUrl,
        NotificationTarget notificationTarget,
        NotificationType notificationType
    )
    {
        return new Notification(
            NotificationId.CreateUnique(),
            recipientId,
            actorId,
            actorName,
            actorAvatarUrl,
            notificationTarget,
            notificationType
        );
    }

    public void MarkAsRead ()
    {
        ReadAt = DateTime.UtcNow;
    }

    public string GetFormattedPayload ()
    {
        return $"{ActorName ?? "System"} has {NotificationTarget} to you";
    }
}
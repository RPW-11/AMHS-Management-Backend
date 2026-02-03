namespace Application.DTOs.Notification;

public record class NotificationDto(
    string Id,
    string? ActorAvatarUrl,
    string ActorName,
    string Message,
    string TargetType,
    string TargetId,
    bool IsRead,
    DateTime CreatedAt
);

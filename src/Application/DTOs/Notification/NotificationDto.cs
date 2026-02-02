namespace Application.DTOs.Notification;

public record class NotificationDto(
    string Id,
    string? ActorAvatarUrl,
    string ActorName,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAt
);

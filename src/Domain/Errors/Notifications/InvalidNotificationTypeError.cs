namespace Domain.Errors.Notifications;

public class InvalidNotificationType : DomainError
{
    public InvalidNotificationType(string notificationType) 
        : base("Invalid notification type", "Notification.InvalidNotificationType", $"The {notificationType} type is not valid")
    {
    }
}

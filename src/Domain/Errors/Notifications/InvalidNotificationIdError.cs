namespace Domain.Errors.Notifications;

public class InvalidNotificationIdError : DomainError
{
    public InvalidNotificationIdError(string id) 
        : base("Invalid notification id", "Notification.InvalidNotificationId", $"The {id} is not valid")
    {
    }
}

using Application.DTOs.Notification;
using Domain.Employees.ValueObjects;

namespace Application.Common.Interfaces.RealTime;

public interface INotificationPublisher
{
    /// <summary>
    /// Publishes a notification to a specific user's SSE stream (if connected)
    /// </summary>
    Task PublishToUserAsync(EmployeeId employeeId, NotificationDto notification);

    /// <summary>
    /// Publishes to multiple users at once
    /// </summary>
    Task PublishToUsersAsync(IEnumerable<EmployeeId> employeeIds, NotificationDto notification);
}
using Application.DTOs.Common;
using Application.DTOs.Notification;
using Domain.Employees.ValueObjects;
using Domain.Notifications;
using Domain.Notifications.ValueObjects;
using FluentResults;

namespace Application.Common.Interfaces.Persistence;

public interface INotificationRepository
{
    Task<Result> AddNotificationAsync(Notification notification);
    Task<Result> AddNotificationsAsync(IEnumerable<Notification> notifications);
    Task<Result<PagedResult<Notification>>> GetNotificationsByEmployeeIdAsync(NotificationFilterDto notificationFilterDto);
    Task<Result<Notification?>> GetNotificationByIdAsync(NotificationId notificationId);
    Result UpdateNotificationAsync(Notification notification);
    Task<Result> DeleteNotificationAsync(NotificationId notificationId);
}
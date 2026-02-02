using Application.DTOs.Common;
using Domain.Employees.ValueObjects;
using Domain.Notifications;
using Domain.Notifications.ValueObjects;
using FluentResults;

namespace Application.Common.Interfaces.Persistence;

public interface INotificationRepository
{
    Task<Result> AddNotificationAsync(Notification notification);
    Task<Result<PagedResult<Notification>>> GetNotificationsByEmployeeIdAsync(EmployeeId employeeId, int page, int pageSize);
    Task<Result<Notification?>> GetNotificationByIdAsync(NotificationId notificationId);
    Task<Result> UpdateNotificationAsync(Notification notification);
    Task<Result> DeleteNotificationAsync(NotificationId notificationId);
}
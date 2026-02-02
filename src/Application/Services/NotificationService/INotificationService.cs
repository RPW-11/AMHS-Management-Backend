using Application.DTOs.Common;
using Application.DTOs.Notification;
using FluentResults;

namespace Application.Services.NotificationService;

public interface INotificationService
{
    Task<Result<PagedResult<NotificationDto>>> GetNotificationsByEmployeeIdAsync(string employeeId, int page, int pageSize);
    Task<Result> DeleteNotificationAsync(string requesterId, string notificationId);
    Task<Result> MarkNotificationAsReadAsync(string requesterId, string notificationId);
}
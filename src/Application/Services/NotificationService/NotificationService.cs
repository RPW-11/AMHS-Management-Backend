using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Persistence;
using Application.DTOs.Common;
using Application.DTOs.Notification;
using Domain.Employees.ValueObjects;
using Domain.Notifications;
using Domain.Notifications.ValueObjects;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Application.Services.NotificationService;

public class NotificationService : BaseService, INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IUnitOfWork unitOfWork,
        INotificationRepository notificationRepository,
        ILogger<NotificationService> logger
    ) : base(unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task<Result<PagedResult<NotificationDto>>> GetNotificationsByEmployeeIdAsync(string employeeId, int page, int pageSize)
    {
        var employeeIdResult = EmployeeId.FromString(employeeId);
        if (employeeIdResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation("Invalid employee id"));
        }
        
        var pagedNotificationsResult = await _notificationRepository.GetNotificationsByEmployeeIdAsync(employeeIdResult.Value, page, pageSize);
        if (pagedNotificationsResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Internal);
        }

        List<NotificationDto> notificationDtos = [.. pagedNotificationsResult.Value.Items.Select(ToNotificationDto)];

        var pagedResult = new PagedResult<NotificationDto>
        {
            Items = notificationDtos,
            Page = pagedNotificationsResult.Value.Page,
            PageSize = pagedNotificationsResult.Value.PageSize,
            TotalCount = pagedNotificationsResult.Value.TotalCount
        };

        return Result.Ok(pagedResult);
    }

    public async Task<Result> MarkNotificationAsReadAsync(string requesterId, string notificationId)
    {
        var notificationIdResult = NotificationId.FromString(notificationId);
        if (notificationIdResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation("Invalid notification id"));
        }

        var notificationResult = await _notificationRepository.GetNotificationByIdAsync(notificationIdResult.Value);
        if (notificationResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Internal);
        }

        var notification = notificationResult.Value;
        if (notification is null)
        {
            return Result.Fail(ApplicationError.NotFound("Notification not found"));
        }

        if (notification.RecipientId.ToString() != requesterId)
        {
            return Result.Fail(ApplicationError.Forbidden("You are not allowed to mark this notification as read"));
        }

        notification.MarkAsRead();

        var updateResult = await _notificationRepository.UpdateNotificationAsync(notification);
        if (updateResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Internal);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Notification successfully updated");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database commit failed while updating notification");
            return Result.Fail(ApplicationError.Internal);
        }
    }

    public async Task<Result> DeleteNotificationAsync(string requesterId, string notificationId)
    {
        var notificationIdResult = NotificationId.FromString(notificationId);
        if (notificationIdResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation("Invalid notification id"));
        }

        var notificationResult = await _notificationRepository.GetNotificationByIdAsync(notificationIdResult.Value);
        if (notificationResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Internal);
        }

        var notification = notificationResult.Value;
        if (notification is null)
        {
            return Result.Fail(ApplicationError.NotFound("Notification not found"));
        }

        if (notification.RecipientId.ToString() != requesterId)
        {
            return Result.Fail(ApplicationError.Forbidden("You are not allowed to delete this notification"));
        }

        var deleteResult = await _notificationRepository.DeleteNotificationAsync(notificationIdResult.Value);
        if (deleteResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Internal);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Notification successfully deleted");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database commit failed while deleting notification");
            return Result.Fail(ApplicationError.Internal);
        }
    }

    private static NotificationDto ToNotificationDto(Notification notification)
    {
        return new NotificationDto(
                    Id: notification.Id.ToString(),
                    ActorAvatarUrl: notification.ActorAvatarUrl,
                    ActorName: notification.ActorName,
                    Message: notification.Payload,
                    TargetId: notification.NotificationTarget.Id.ToString(),
                    TargetType: notification.NotificationTarget.Type,
                    IsRead: notification.ReadAt.HasValue,
                    CreatedAt: notification.CreatedAt
                );
    }
}
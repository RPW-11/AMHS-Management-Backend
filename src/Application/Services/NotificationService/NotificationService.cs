using System.Runtime.CompilerServices;
using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.RealTime;
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
    private readonly INotificationHub _notificationHub;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IUnitOfWork unitOfWork,
        INotificationRepository notificationRepository,
        INotificationHub notificationHub,
        ILogger<NotificationService> logger
    ) : base(unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
        _notificationHub = notificationHub;
    }

    public async Task<Result<PagedResult<NotificationDto>>> GetNotificationsByEmployeeIdAsync( string employeeId, int page, int pageSize, string? type)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Page"] = page,
            ["PageSize"] = pageSize,
            ["EmployeeId"] = employeeId,
            ["HasType"] = !string.IsNullOrWhiteSpace(type),
        });

        _logger.LogInformation("Get all missions paged request started");
        _logger.LogInformation("Notification filter: Page = {page} | Page Size = {pageSize} | Type = {type}", page, pageSize, type);


        var filterResult = NotificationFilterDto.Create(page, pageSize, employeeId, type);
        if (filterResult.IsFailed)
        {
            return Result.Fail<PagedResult<NotificationDto>>(ApplicationError.Validation(filterResult.Errors[0].Message));
        }

        var pagedNotificationsResult = await _notificationRepository.GetNotificationsByEmployeeIdAsync(filterResult.Value);
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

        var updateResult = _notificationRepository.UpdateNotificationAsync(notification);
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

    public Result CreateChannel(string requesterId)
    {
        var employeeIdResult = EmployeeId.FromString(requesterId);
        if (employeeIdResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation("Invalid requester id"));
        }

        _notificationHub.CreateChannel(employeeIdResult.Value);

        return Result.Ok();
    }

    public async IAsyncEnumerable<Result<string>> ReadAllAsync(
        string requesterId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var employeeIdResult = EmployeeId.FromString(requesterId);
        if (employeeIdResult.IsFailed)
        {
            yield return Result.Fail(ApplicationError.Validation("Invalid requester id"));
        }

        await foreach (var message in _notificationHub.ReadFromChannelAsync(employeeIdResult.Value, cancellationToken))
        {
            yield return message;
        }

        yield return "";
    }

    public Result DeleteChannel(string requesterId)
    {
        var employeeIdResult = EmployeeId.FromString(requesterId);
        if (employeeIdResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation("Invalid requester id"));
        }
        _notificationHub.TryRemoveChannel(employeeIdResult.Value);

        return Result.Ok();
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
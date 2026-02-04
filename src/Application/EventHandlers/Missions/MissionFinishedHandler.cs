using Application.Common.Interfaces;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.RealTime;
using Application.DTOs.Notification;
using Domain.Missions.Events;
using Domain.Notifications;
using Domain.Notifications.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers.Missions;
public class MissionFinishedHandler : IDomainEventHandler<MissionFinishedEvent>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<MissionFinishedHandler> _logger;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly IUnitOfWork _unitOfWork;

    public MissionFinishedHandler(
        INotificationRepository notificationRepository,
        ILogger<MissionFinishedHandler> logger,
        INotificationPublisher notificationPublisher,
        IUnitOfWork unitOfWork
    )
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
        _notificationPublisher = notificationPublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(MissionFinishedEvent evt)
    {
        List<Notification> notifications = [];
        foreach (var memberId in evt.AssignedEmployeeIds)
        {
            var notification = Notification.Create(
                memberId,
                null,
                "System",
                null,
                NotificationTarget.Create(evt.MissionId.Value, "MISSION"),
                NotificationType.MissionFinished,
                $"Mission '{evt.MissionName}' has been finished"
            );
            notifications.Add(notification.Value);
        }

        var notificationsAddResult = await _notificationRepository.AddNotificationsAsync(notifications);
        if (notificationsAddResult.IsFailed){
            _logger.LogError("Failed to add notifications for mission finished event | MissionId: {MissionId} | Errors: {Errors}", evt.MissionId, notificationsAddResult.Errors);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add notifications for mission finished event | MissionId: {MissionId}", evt.MissionId);
        }

        // publish
        await _notificationPublisher.PublishToUsersAsync([.. evt.AssignedEmployeeIds], ToNotificationDto(notifications.First()));

        _logger.LogInformation("Successfully added notifications for mission finished event | MissionId: {MissionId}", evt.MissionId);
    }

    private static NotificationDto ToNotificationDto(Notification notification)
    {
        return new NotificationDto(
            notification.Id.ToString(),
            notification.ActorAvatarUrl,
            notification.ActorName,
            notification.Payload,
            notification.NotificationTarget.Type,
            notification.NotificationTarget.Id.ToString(),
            notification.ReadAt is not null,
            notification.CreatedAt
        );
    }   
}
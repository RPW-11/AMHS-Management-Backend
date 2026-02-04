using System.Text.Json;
using Application.Common.Interfaces.RealTime;
using Application.DTOs.Notification;
using Domain.Employees.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Infrastructure.RealTime;

public class NotificationPublisher : INotificationPublisher
{
    private readonly INotificationHub _hub;
    private readonly ILogger<NotificationPublisher> _logger;

    public NotificationPublisher(INotificationHub hub, ILogger<NotificationPublisher> logger)
    {
        _hub = hub;
        _logger = logger;
    }
    public async Task PublishToUserAsync(EmployeeId employeeId, NotificationDto notification)
    {
        try
        {
            var json = JsonSerializer.Serialize(notification);
            var sseMessage = $"data: {json}\n\n";
            await _hub.PublishToUserAsync(employeeId, sseMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish SSE to employee {EmpId}", employeeId);
        }
    }

    public async Task PublishToUsersAsync(IEnumerable<EmployeeId> employeeIds, NotificationDto notification)
    {
        foreach (var employeeId in employeeIds)
        {
            await PublishToUserAsync(employeeId, notification);
        }
    }
}
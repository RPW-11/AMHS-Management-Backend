using System.Security.Claims;
using Application.DTOs.Common;
using Application.DTOs.Notification;
using Application.Services.NotificationService;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    public class NotificationController : ApiBaseController
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;
        public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpGet("stream")]
        public async Task StreamNotifications()
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (employeeId is null)
            {
                Response.StatusCode = 400;
                await Response.WriteAsync("token is required");
                return;
            }

            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("X-Accel-Buffering", "no"); // important for nginx / reverse proxy

            var createChannelResult = _notificationService.CreateChannel(employeeId);
            if (createChannelResult.IsFailed)
            {
                await Response.WriteAsync("Invalid employee id");
                return;
            }

            _logger.LogInformation("User {employeeId} has connected", employeeId);

            try
            {
                // Heartbeat every 20 seconds to keep connection alive
                using var cts = new CancellationTokenSource();
                var heartbeatTask = Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        await Response.WriteAsync(": heartbeat\n\n");
                        await Response.Body.FlushAsync();
                        await Task.Delay(20000, cts.Token);
                    }
                }, cts.Token);

                await foreach(var readResult in _notificationService.ReadAllAsync(employeeId, HttpContext.RequestAborted))
                {
                    if (readResult.IsFailed)
                    {
                        await Response.WriteAsync("Invalid employee id");
                        return; 
                    }
                    await Response.WriteAsync(readResult.Value);
                    await Response.Body.FlushAsync();
                }

                cts.Cancel();
            }
            catch (OperationCanceledException)
            {
                // Client disconnected normally
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SSE connection error for user {employeeId}", employeeId);
            }
            finally
            {
                _notificationService.DeleteChannel(employeeId);
                _logger.LogInformation("SSE connection closed for user {employeeId}", employeeId);
            }
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<NotificationDto>>> Get(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20
        )
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (employeeId is null)
            {
                return Problem(statusCode: 404, title: "Employee not found");
            }

            var notificationsResult = await _notificationService.GetNotificationsByEmployeeIdAsync(employeeId, page, pageSize);

            return HandleResult(notificationsResult);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (employeeId is null)
            {
                return Problem(statusCode: 404, title: "Employee not found");
            }

            FluentResults.Result<object> deleteResult = await _notificationService.DeleteNotificationAsync(employeeId, id);

            return HandleResult(deleteResult);
        }

        [HttpPatch("{id}/mark-as-read")]
        public async Task<ActionResult> MarkAsRead(string id)
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (employeeId is null)
            {
                return Problem(statusCode: 404, title: "Employee not found");
            }

            FluentResults.Result<object> updateResult = await _notificationService.MarkNotificationAsReadAsync(employeeId, id);

            return HandleResult(updateResult);
        }
    }
}

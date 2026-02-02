using System.Security.Claims;
using Application.DTOs.Common;
using Application.DTOs.Notification;
using Application.Services.NotificationService;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/notification")]
    [ApiController]
    public class NotificationController : ApiBaseController
    {
        private readonly INotificationService _notificationService;
        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
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

using Application.DTOs.Common;
using Application.DTOs.Notification;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/notification")]
    [ApiController]
    public class NotificationController : ApiBaseController
    {
        public NotificationController()
        {
            
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<NotificationDto>>> Get()
        {
            return Ok();
        }

        

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            return Ok();
        }
    }
}

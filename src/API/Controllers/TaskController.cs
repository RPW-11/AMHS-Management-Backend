using API.Contracts.Task;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/tasks")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        /// <summary>
        /// Create route planning task
        /// </summary>
        [HttpPost("route-planning")]
        public ActionResult CreateRoutePlanningTask(CreateRoutePlanningRequest createRoutePlanningRequest)
        {
            return Created();
        }
    }
}

using API.Contracts.Task;
using Application.DTOs.Mission.RoutePlanning;
using Application.Services.TaskService.RoutePlanningService;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/tasks")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly IRoutePlanningService _routePlanningService;

        public TaskController(IRoutePlanningService routePlanningService)
        {
            _routePlanningService = routePlanningService;
        }

        /// <summary>
        /// Create route planning task
        /// </summary>
        /// <remarks>
        /// 
        /// Sample request:
        /// 
        ///     POST /tasks/route-planning
        ///     {
        ///          "image": "string",
        ///          "rowDim": 6,
        ///          "colDim": 6,
        ///          "algorithm": "string",
        ///          "stationsOrder": [
        ///              {
        ///              "rowPos": 5,
        ///              "colPos": 0
        ///              },
        ///              {
        ///              "rowPos": 0,
        ///              "colPos": 4
        ///              }
        ///          ],
        ///          "points": [
        ///              {
        ///              "name": "string",
        ///              "category": "OBS",
        ///              "time": 0,
        ///              "position": {
        ///                  "rowPos": 0,
        ///                  "colPos": 0
        ///              }
        ///              },
        ///              {
        ///              "name": "string",
        ///              "category": "OBS",
        ///              "time": 0,
        ///              "position": {
        ///                  "rowPos": 1,
        ///                  "colPos": 1
        ///              }
        ///              },
        ///              {
        ///              "name": "string",
        ///              "category": "OBS",
        ///              "time": 0,
        ///              "position": {
        ///                  "rowPos": 1,
        ///                  "colPos": 2
        ///              }
        ///              },
        ///              {
        ///              "name": "string",
        ///              "category": "OBS",
        ///              "time": 0,
        ///              "position": {
        ///                  "rowPos": 0,
        ///                  "colPos": 3
        ///              }
        ///              },
        ///              {
        ///              "name": "string",
        ///              "category": "OBS",
        ///              "time": 0,
        ///              "position": {
        ///                  "rowPos": 0,
        ///                  "colPos": 5
        ///              }
        ///              },
        ///              {
        ///              "name": "string",
        ///              "category": "OBS",
        ///              "time": 0,
        ///              "position": {
        ///                  "rowPos": 1,
        ///                  "colPos": 5
        ///              }
        ///              },
        ///              {
        ///              "name": "string",
        ///              "category": "OBS",
        ///              "time": 0,
        ///              "position": {
        ///                  "rowPos": 2,
        ///                  "colPos": 4
        ///              }
        ///              },
        ///              {
        ///              "name": "string",
        ///              "category": "OBS",
        ///              "time": 0,
        ///              "position": {
        ///                  "rowPos": 3,
        ///                  "colPos": 4
        ///              }
        ///              },
        ///              {
        ///              "name": "string",
        ///              "category": "OBS",
        ///              "time": 0,
        ///              "position": {
        ///                  "rowPos": 4,
        ///                  "colPos": 1
        ///              }
        ///              },
        ///              {
        ///              "name": "string",
        ///              "category": "OBS",
        ///              "time": 0,
        ///              "position": {
        ///                  "rowPos": 4,
        ///                  "colPos": 2
        ///              }
        ///              },
        ///              {
        ///              "name": "string",
        ///              "category": "OBS",
        ///              "time": 0,
        ///              "position": {
        ///                  "rowPos": 5,
        ///                  "colPos": 4
        ///              }
        ///              }
        ///          ]
        ///          }
        /// </remarks>
        [HttpPost("route-planning")]
        public ActionResult CreateRoutePlanningTask(CreateRoutePlanningRequest createRoutePlanningRequest)
        {
            List<PathPointDto> points = [];
            List<PointPositionDto> stations = [];

            foreach (var point in createRoutePlanningRequest.Points)
            {
                points.Add(new(
                    Name: point.Name,
                    Category: point.Category,
                    Position: new (point.Position.RowPos, point.Position.ColPos),
                    Time: point.Time
                ));
            }

            foreach (var point in createRoutePlanningRequest.StationsOrder)
            {
                stations.Add(new(point.RowPos, point.ColPos));
            }

            var routeResult = _routePlanningService.FindRgvBestRoute(
                createRoutePlanningRequest.RowDim,
                createRoutePlanningRequest.ColDim,
                points,
                stations
            );

            if (routeResult.IsFailed)
            {
                var error = routeResult.Errors[0];
                return Problem(title: error.Message, statusCode: (int)error.Metadata["statusCode"], detail: (string)error.Metadata["detail"]);
            }

            return Created();
        }
    }
}

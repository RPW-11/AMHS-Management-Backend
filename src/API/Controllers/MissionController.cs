using System.Net;
using System.Security.Claims;
using System.Text.Json;
using API.Contracts.Mission;
using Application.DTOs.Common;
using Application.DTOs.Mission;
using Application.DTOs.Mission.RoutePlanning;
using Application.Services.MissionService;
using Application.Services.MissionService.RoutePlanningService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/missions")]
    [ApiController]
    [Authorize]
    public class MissionController : ApiBaseController
    {
        private readonly IMissionService _missionService;
        private readonly IRoutePlanningService _routePlanningService;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        public MissionController(IRoutePlanningService routePlanningService, IMissionService missionService)
        {
            _missionService = missionService;
            _routePlanningService = routePlanningService;
            _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        /// <summary>
        /// Get all missions
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<MissionDto>>> GetAllMissions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchTerm = null
        )
        {
            var missionsResult = await _missionService.GetAllMission(page, pageSize, searchTerm);

            return HandleResult(missionsResult);
        }

        /// <summary>
        /// Get a mission by id
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<MissionDto>> GetMission(string id)
        {
            var missionResult = await _missionService.GetMission(id);

            return HandleResult(missionResult);
        }

        /// <summary>
        /// Get all missions associated with the current logged-in user.
        /// </summary>
        [HttpGet("me")]
        public async Task<ActionResult<PagedResult<MissionDto>>> GetMyMissions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchTerm = null
        )
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (employeeId is null)
            {
                return Problem(statusCode: 404, title: "Employee not found");
            }

            var missionsResult = await _missionService.GetAllMissionsByEmployeeId(employeeId, page, pageSize, searchTerm);

            return HandleResult(missionsResult);
        }

        /// <summary>
        /// Get the mission members
        /// </summary>
        [HttpGet("{id}/members")]
        public async Task<ActionResult<IEnumerable<MissionDto>>> GetMissionMembers(string id)
        {
            var missionMembersResult = await _missionService.GetMissionMembers(id);

            return HandleResult(missionMembersResult);
        }

        /// <summary>
        /// Add a mission
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<AddMissionDto>> AddMission(AddMissionRequest addMissionRequest)
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (employeeId is null)
            {
                return Problem(statusCode: 404, title: "Employee not found");
            }

            FluentResults.Result<AddMissionDto> addMissionResult = await _missionService.AddMission(
                employeeId.ToString(),
                addMissionRequest.Name,
                addMissionRequest.Category,
                addMissionRequest.Description,
                addMissionRequest.FinishedAt
            );

            if (addMissionResult.IsFailed)
            {
                return HandleResult(addMissionResult);
            }

            return Ok(addMissionResult.Value);
        }

        /// <summary>
        /// Update mission
        /// </summary>
        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateMission(UpdateMissionRequest updateMissionRequest, string id)
        {
            var updateMissionDto = new UpdateMissionDto(
                updateMissionRequest.Name,
                updateMissionRequest.Description,
                updateMissionRequest.Status
            );

            FluentResults.Result<object> updateMissionResult = await _missionService.UpdateMission(updateMissionDto, id);

            return HandleResult(updateMissionResult);
        }

        /// <summary>
        /// Delete mission
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMission(string id)
        {
            FluentResults.Result<object> deleteMissionResult = await _missionService.DeleteMission(id);

            return HandleResult(deleteMissionResult);
        }

        /// <summary>
        /// Delete missions in bulk
        /// </summary>
        [HttpDelete]
        public async Task<ActionResult> DeleteMissions(DeleteMissionsRequest deleteMissionsRequest)
        {
            Console.WriteLine(deleteMissionsRequest.MissionIds);
            FluentResults.Result<object> deleteMissionsResult = await _missionService.DeleteMissions(deleteMissionsRequest.MissionIds);
            return HandleResult(deleteMissionsResult);
        }

        /// <summary>
        /// Add a member to a mission
        /// </summary>
        [HttpPatch("{id}/members/add/{memberId}")]
        public async Task<ActionResult> AddMemberToMissionHandler(string id, string memberId)
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (employeeId is null)
            {
                return Problem(statusCode: 404, title: "Employee not found");
            }

            FluentResults.Result<object> addMemberResult = await _missionService.AddMemberToMission(employeeId, id, memberId);

            return HandleResult(addMemberResult);
        }

        /// <summary>
        /// Delete a member to a mission
        /// </summary>
        [HttpPatch("{id}/members/delete/{memberId}")]
        public async Task<ActionResult> DeleteMemberToMissionHandler(string id, string memberId)
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (employeeId is null)
            {
                return Problem(statusCode: 404, title: "Employee not found");
            }

            FluentResults.Result<object> deleteMemberResult = await _missionService.DeleteMemberFromMission(employeeId, id, memberId);

            return HandleResult(deleteMemberResult);
        }

        /// <summary>
        /// Change the role of a member in a mission
        /// </summary>
        [HttpPatch("{id}/members/changeRole/{memberId}")]
        public async Task<ActionResult> ChangeRoleMemberHandler(string id, string memberId, ChangeMemberRoleRequest changeMemberRoleRequest)
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (employeeId is null)
            {
                return Problem(statusCode: 404, title: "Employee not found");
            }

            FluentResults.Result<object> changeMemberRoleResult = await _missionService.ChangeMemberRole(employeeId, id, memberId, changeMemberRoleRequest.Role);

            return HandleResult(changeMemberRoleResult);
        }


        /// <summary>
        /// Update the created route planning task with the required data
        /// </summary>
    
        [HttpPatch("{id}/route-planning")]
        public async Task<ActionResult> CreateRoutePlanningTask(
            string id,
            [FromForm] CreateRoutePlanningRequest createRoutePlanningRequest
        )
        {
            RouteMetadata? routeMetadata = JsonSerializer.Deserialize<RouteMetadata>(createRoutePlanningRequest.RouteMetadata, _jsonSerializerOptions);

            if (routeMetadata == null)
            {
                return BadRequest();
            }

            List<PathPointDto> points = [];
            List<PointPositionDto> stations = [];
            List<List<PointPositionDto>> sampleSolutions = [];

            foreach (var point in routeMetadata.Points)
            {
                points.Add(new(
                    Name: point.Name,
                    Category: point.Category,
                    Position: new(point.Position.RowPos, point.Position.ColPos),
                    Time: point.Time
                ));
            }

            foreach (var point in routeMetadata.StationsOrder)
            {
                stations.Add(new(point.RowPos, point.ColPos));
            }

            foreach (var sol in routeMetadata.SampleSolutions)
            {
                List<PointPositionDto> temp = [];
                foreach (var point in sol)
                {
                    temp.Add(new(point.RowPos, point.ColPos));
                }
                sampleSolutions.Add(temp);
            }

            using (var imageStream = new MemoryStream())
            {
                createRoutePlanningRequest.Image.CopyTo(imageStream);
                imageStream.Seek(0, SeekOrigin.Begin);

                var routeResult = await _routePlanningService.FindRgvBestRoute(
                    id,
                    imageStream,
                    routeMetadata.Algorithm,
                    routeMetadata.RowDim,
                    routeMetadata.ColDim,
                    routeMetadata.WidthLength,
                    routeMetadata.HeightLength,
                    points,
                    stations,
                    sampleSolutions
                );

                if (routeResult.IsFailed)
                {
                    var error = routeResult.Errors[0];
                    return Problem(
                        title: error.Message,
                        statusCode: (int)HttpStatusCode.BadRequest,
                        detail: (string)error.Metadata["detail"]
                    );
                }
            }

            return Created();
        }
    }
}

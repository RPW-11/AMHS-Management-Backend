using System.Net;
using System.Security.Claims;
using System.Text.Json;
using API.Contracts.Mission;
using Application.DTOs.Common;
using Application.DTOs.Mission;
using Application.DTOs.RoutePlanning;
using Application.Services.MissionService;
using Application.Services.RoutePlanningService;
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
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Number of missions per page.</param>
        /// <param name="status">Optional mission status filter.</param>
        /// <param name="employeeId">Optional filter to missions assigned to a specific employee.</param>
        /// <param name="name">Optional filter matched against mission name.</param>
        [HttpGet]
        public async Task<ActionResult<PagedResult<MissionDto>>> GetAllMissions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] string? employeeId = null,
            [FromQuery] string? name = null

        )
        {
            var missionsResult = await _missionService.GetAllMission(page, pageSize, status, name, employeeId);

            return HandleResult(missionsResult);
        }

        /// <summary>
        /// Get a mission by id
        /// </summary>
        /// <param name="id">The mission id.</param>
        [HttpGet("{id}")]
        public async Task<ActionResult<MissionDetailDto>> GetMission(string id)
        {
            var missionResult = await _missionService.GetMission(id);

            return HandleResult(missionResult);
        }

        /// <summary>
        /// Get all missions associated with the current logged-in user.
        /// </summary>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Number of missions per page.</param>
        /// <param name="status">Optional mission status filter.</param>
        /// <param name="name">Optional filter matched against mission name.</param>
        [HttpGet("me")]
        public async Task<ActionResult<PagedResult<MissionDto>>> GetMyMissions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] string? name = null
        )
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (employeeId is null)
            {
                return Problem(statusCode: 404, title: "Employee not found");
            }

            var missionsResult = await _missionService.GetAllMission(page, pageSize, status, name, employeeId);

            return HandleResult(missionsResult);
        }

        /// <summary>
        /// Get the mission members
        /// </summary>
        /// <param name="id">The mission id.</param>
        [HttpGet("{id}/members")]
        public async Task<ActionResult<IEnumerable<MissionDto>>> GetMissionMembers(string id)
        {
            var missionMembersResult = await _missionService.GetMissionMembers(id);

            return HandleResult(missionMembersResult);
        }

        /// <summary>
        /// Add a mission
        /// </summary>
        /// <remarks>
        /// The current logged-in user (from the JWT claims) is set as the mission's owner/leader.
        /// </remarks>
        /// <param name="addMissionRequest">The new mission's details.</param>
        /// <returns>The created mission on success.</returns>
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
        /// <param name="updateMissionRequest">The fields to update.</param>
        /// <param name="id">The mission id.</param>
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
        /// <param name="id">The mission id.</param>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMission(string id)
        {
            FluentResults.Result<object> deleteMissionResult = await _missionService.DeleteMission(id);

            return HandleResult(deleteMissionResult);
        }

        /// <summary>
        /// Delete missions in bulk
        /// </summary>
        /// <param name="deleteMissionsRequest">The ids of the missions to delete.</param>
        [HttpDelete]
        public async Task<ActionResult> DeleteMissions(DeleteMissionsRequest deleteMissionsRequest)
        {
            FluentResults.Result<object> deleteMissionsResult = await _missionService.DeleteMissions(deleteMissionsRequest.MissionIds);
            return HandleResult(deleteMissionsResult);
        }

        /// <summary>
        /// Add a member to a mission
        /// </summary>
        /// <remarks>
        /// The current logged-in user must be the mission's leader or co-leader.
        /// </remarks>
        /// <param name="id">The mission id.</param>
        /// <param name="memberId">The employee id to add.</param>
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
        /// Delete a member from a mission
        /// </summary>
        /// <remarks>
        /// The current logged-in user must be the mission's leader or co-leader, and cannot remove themselves.
        /// </remarks>
        /// <param name="id">The mission id.</param>
        /// <param name="memberId">The employee id to remove.</param>
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
        /// <remarks>
        /// The current logged-in user must be the mission's leader (co-leaders cannot change roles),
        /// and cannot change their own role.
        /// </remarks>
        /// <param name="id">The mission id.</param>
        /// <param name="memberId">The employee id whose role is being changed.</param>
        /// <param name="changeMemberRoleRequest">The new role to assign.</param>
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
        /// Get a downloadable URL for the result image of a finished route planning task
        /// </summary>
        /// <remarks>
        /// Only supported when the server is configured with the S3 route planning result store —
        /// the returned URL is a presigned S3 GET URL (valid for 1 hour) with a
        /// Content-Disposition: attachment override, so opening it downloads rather than renders
        /// the image. Not available when the local disk result store is configured.
        /// </remarks>
        /// <param name="id">The mission id.</param>
        /// <returns>A JSON object containing the downloadable image URL.</returns>
        [HttpGet("{id}/route-planning/image")]
        public async Task<ActionResult<RouteImageUrlDto>> DownloadRouteImage(string id)
        {
            var imageUrlResult = await _missionService.DownloadRouteImage(id);
            if (imageUrlResult.IsFailed)
            {
                return HandleResult(imageUrlResult);
            }

            return Ok(new RouteImageUrlDto(imageUrlResult.Value));
        }

        /// <summary>
        /// Enqueue a route planning solve for a mission
        /// </summary>
        /// <remarks>
        /// This request returns as soon as the solve job is enqueued, not once it has finished
        /// solving — the mission is flipped to "Processing" and the actual GA/A* solve runs
        /// asynchronously in the background. Poll <c>GET /api/missions/{id}</c> or listen for the
        /// "MissionFinished"/"MissionRoutePlanningStarted" notifications to know when it's done.
        /// </remarks>
        /// <param name="id">The mission id.</param>
        /// <param name="createRoutePlanningRequest">
        /// The source factory layout image plus route metadata (points, clusters, cluster flows,
        /// algorithm) as multipart form data.
        /// </param>
        /// <returns>201 Created once the solve job has been enqueued.</returns>
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

            var points = routeMetadata.Points;
            var clusters = routeMetadata.Clusters;
            var clusterFlows = routeMetadata.ClusterFlows;

            byte[] imageBytes;
            using (var imageStream = new MemoryStream())
            {
                createRoutePlanningRequest.Image.CopyTo(imageStream);
                imageBytes = imageStream.ToArray();
            }

            var routeResult = await _routePlanningService.EnqueueRoutePlanning(new RoutePlanningRequest(
                id,
                imageBytes,
                routeMetadata.Algorithm,
                routeMetadata.RowDim,
                routeMetadata.ColDim,
                routeMetadata.WidthLength,
                routeMetadata.HeightLength,
                points,
                clusters,
                clusterFlows
            ));

            if (routeResult.IsFailed)
            {
                var error = routeResult.Errors[0];
                return Problem(
                    title: error.Message,
                    statusCode: (int)HttpStatusCode.BadRequest,
                    detail: (string)error.Metadata["detail"]
                );
            }

            return Created();
        }
    }
}

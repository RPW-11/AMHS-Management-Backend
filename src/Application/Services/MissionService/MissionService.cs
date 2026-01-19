using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.Common;
using Application.DTOs.Mission;
using Application.DTOs.Mission.RoutePlanning;
using Domain.Employee;
using Domain.Employee.ValueObjects;
using Domain.Mission;
using Domain.Mission.ValueObjects;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Application.Services.MissionService;

public class MissionService : BaseService, IMissionService
{
    private readonly IMissionRepository _missionRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IRgvRoutePlanning _rgvRoutePlanning;
    private readonly ILogger<MissionService> _logger;

    public MissionService(IMissionRepository missionRepository,
                          IEmployeeRepository employeeRepository,
                          IRgvRoutePlanning rgvRoutePlanning,
                          IUnitOfWork unitOfWork,
                          ILogger<MissionService> logger)
    : base(unitOfWork)
    {
        _missionRepository = missionRepository;
        _employeeRepository = employeeRepository;
        _rgvRoutePlanning = rgvRoutePlanning;
        _logger = logger;
    }

    public async Task<Result<AddMissionDto>> AddMission(string employeeId,
                                                        string name,
                                                        string category,
                                                        string description,
                                                        DateTime finishedAt)
    {
        var employeeIdResult = EmployeeId.FromString(employeeId);
        if (employeeIdResult.IsFailed)
        {
            _logger.LogWarning("{msg}", employeeIdResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation("Invalid employee id"));
        }

        var existing = await _employeeRepository.GetEmployeeByIdAsync(employeeIdResult.Value);
        if (existing is null)
        {
            _logger.LogWarning("Employee with id {employeeId} does not exist", employeeId);
            return Result.Fail(ApplicationError.Validation("Non employee can't create a mission"));
        }

        var missionDomainResult = MissionFactory.CreateMission(employeeId,
                                                               name,
                                                               category,
                                                               description,
                                                               finishedAt);

        if (missionDomainResult.IsFailed)
        {
            _logger.LogWarning("{msg}", missionDomainResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation(missionDomainResult.Errors[0].Message));
        }

        var addMissionResult = await _missionRepository.AddMissionAsync(missionDomainResult.Value);

        if (addMissionResult.IsFailed)
        {
            _logger.LogError("Failed to add mission to repository: {msg}", addMissionResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        await _unitOfWork.SaveChangesAsync();

        return new AddMissionDto(missionDomainResult.Value.Id.ToString());
    }

    public async Task<Result<PagedResult<MissionDto>>> GetAllMission(
        int page,
        int pageSize,
        string? searchTerm = null
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 5, 100);

        var missionsCountResult = await _missionRepository.GetMissionsCountAsync();
        if (missionsCountResult.IsFailed)
        {
            _logger.LogError("Failed to get missions count: {msg}", missionsCountResult.Errors[0].Message);
            return Result.Fail<PagedResult<MissionDto>>(ApplicationError.Internal);
        }

        var getMissionsResult = await _missionRepository.GetAllMissionsAsync(page, pageSize);
        if (getMissionsResult.IsFailed)
        {
            _logger.LogError("Failed to get missions: {msg}", getMissionsResult.Errors[0].Message);
            return Result.Fail<PagedResult<MissionDto>>(ApplicationError.Internal);
        }

        List<MissionDto> missionsDto = [];
        foreach (var mission in getMissionsResult.Value)
        {
            missionsDto.Add(MapToMissionDto(mission));
        }

        await _unitOfWork.SaveChangesAsync();

        return new PagedResult<MissionDto>
        {
            Items = missionsDto,
            Page = page,
            PageSize = pageSize,
            TotalCount = missionsCountResult.Value
        };
    }

    public async Task<Result<MissionDetailDto>> GetMission(string id)
    {
        var missionIdResult = MissionId.FromString(id);
        if (missionIdResult.IsFailed)
        {
            _logger.LogWarning("Invalid mission id: {msg}", missionIdResult.Errors[0].Message);
            return Result.Fail<MissionDetailDto>(ApplicationError.Validation(missionIdResult.Errors[0].Message));
        }

        var getMissionResult = await _missionRepository.GetMissionByIdAsync(missionIdResult.Value);

        if (getMissionResult.IsFailed)
        {
            _logger.LogError("Failed to get mission: {msg}", getMissionResult.Errors[0].Message);
            return Result.Fail<MissionDetailDto>(ApplicationError.Internal);
        }

        if (getMissionResult.Value is null)
        {
            _logger.LogWarning("Mission with id {missionId} does not exist", missionIdResult.Value);
            return Result.Fail<MissionDetailDto>(ApplicationError.NotFound("The mission is not found"));
        }

        // get the leader info
        var leader = getMissionResult.Value.AssignedEmployees.FirstOrDefault(ae => ae.MissionRole == MissionRole.Leader);
        if (leader is null) {
            _logger.LogError("Leader not found for mission {missionId}", getMissionResult.Value.Id);
            return Result.Fail<MissionDetailDto>(ApplicationError.NotFound("The mission has no leader"));
        }

        var leaderResult = await _employeeRepository.GetEmployeeByIdAsync(leader.EmployeeId);
        if (leaderResult.IsFailed)
        {
            _logger.LogError("Failed to get leader info: {msg}", leaderResult.Errors[0].Message);
            return Result.Fail<MissionDetailDto>(ApplicationError.Internal);
        }
        if (leaderResult.Value is null)
        {
            _logger.LogWarning("Leader with id {leaderId} does not exist", leader.EmployeeId);
            return Result.Fail<MissionDetailDto>(ApplicationError.NotFound("The leader of this mission does not exist"));
        }

        if (getMissionResult.Value.Category == MissionCategory.RoutePlanning
            && getMissionResult.Value.ResourceLink is not null)
        {
            RoutePlanningSummaryDto routePlanningSummary = _rgvRoutePlanning.ReadFromJson(getMissionResult.Value.ResourceLink);

            // for now, read the image result and send it as base 64
            byte[] imageBytes = File.ReadAllBytes(routePlanningSummary.ImageUrl);
            string base64String = Convert.ToBase64String(imageBytes);

            routePlanningSummary = new RoutePlanningSummaryDto(routePlanningSummary.Algorithm,
                                                               base64String,
                                                               routePlanningSummary.RgvMap,
                                                               routePlanningSummary.Score);
            return MapToMissionDetailDto(getMissionResult.Value, leaderResult.Value, routePlanningSummary);
        }

        await _unitOfWork.SaveChangesAsync();

        return MapToMissionDetailDto(getMissionResult.Value, leaderResult.Value);
    }

    public async Task<Result> UpdateMission(UpdateMissionDto updateMissionDto, string missionId)
    {
        var missionIdResult = MissionId.FromString(missionId);
        if (missionIdResult.IsFailed)
        {
            _logger.LogWarning("Invalid mission id: {msg}", missionIdResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation("Invalid mission id"));
        }

        var missionRepoResult = await _missionRepository.GetMissionByIdAsync(missionIdResult.Value);
        if (missionRepoResult.IsFailed)
        {
            _logger.LogWarning("{msg}",missionRepoResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        if (missionRepoResult.Value is null)
        {
            _logger.LogWarning("Mission with id {missionId} does not exist", missionIdResult.Value);
            return Result.Fail(ApplicationError.NotFound("The specified mission is not found"));
        }

        var statusResult = MissionStatus.FromString(updateMissionDto.Status);
        if (statusResult.IsFailed)
        {
            _logger.LogWarning("Invalid mission status: {msg}", statusResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation("Invalid mission status"));
        }

        missionRepoResult.Value.SetMissionName(updateMissionDto.Name);
        missionRepoResult.Value.SetMissionDescription(updateMissionDto.Description);
        missionRepoResult.Value.SetMissionStatus(statusResult.Value);

        var updateMissionResult = _missionRepository.UpdateMission(missionRepoResult.Value);
        if (updateMissionResult.IsFailed)
        {
            _logger.LogError("Failed to update mission: {msg}", updateMissionResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        await _unitOfWork.SaveChangesAsync();

        return Result.Ok();
    }

    public async Task<Result> DeleteMission(string missionId)
    {
        var missionIdResult = MissionId.FromString(missionId);
        if (missionIdResult.IsFailed)
        {
            _logger.LogWarning("Invalid mission id: {msg}", missionIdResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation("Invalid missionId"));
        }

        var missionResult = await _missionRepository.GetMissionByIdAsync(missionIdResult.Value);
        if (missionResult.IsFailed)
        {
            _logger.LogError("Failed to get mission: {msg}", missionResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        if (missionResult.Value is null)
        {
            return Result.Ok();
        }

        var deleteResult = _missionRepository.DeleteMission(missionResult.Value);
        if (deleteResult.IsFailed)
        {
            _logger.LogError("Failed to delete mission: {msg}", deleteResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Mission with id {missionId} deleted successfully", missionId);
        return Result.Ok();
    }

    public async Task<Result> DeleteMissions(IEnumerable<string> missionIds)
    {
        if (!missionIds.Any())
        {
            return Result.Fail(ApplicationError.NotFound("No mission ids provided"));
        }

        var missionIdsObjs = new List<MissionId>();
        foreach (var missionId in missionIds)
        {
            var missionIdResult = MissionId.FromString(missionId);
            
            if (missionIdResult.IsFailed)
            {
                return Result.Fail(ApplicationError.Validation("Invalid missionId"));
            }

            missionIdsObjs.Add(missionIdResult.Value);
        }

        var deleteMissionsResult = await _missionRepository.DeleteMissionsAsync(missionIdsObjs);

        if (deleteMissionsResult.IsFailed)
        {
            _logger.LogError("Failed to delete mission: {msg}", deleteMissionsResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        _logger.LogInformation("There are {msg} missions deleted", deleteMissionsResult.Value);
        return Result.Ok();
    }

    public async Task<Result> AddMemberToMission(string employeeId, string missionId, string memberId)
    {
        var missionIdResult = MissionId.FromString(missionId);
        if (missionIdResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation("Invalid mission Id"));
        }

        var employeeIdResult = EmployeeId.FromString(employeeId);
        if (employeeIdResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation("Invalid employee Id"));
        }

        var missionResult = await _missionRepository.GetMissionByIdAsync(missionIdResult.Value);
        if (missionResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Internal);
        }
        if (missionResult.Value is null)
        {
            return Result.Fail(ApplicationError.NotFound("Mission is not found"));
        }

        // Check if the requester is valid
        var isRequesterValid = false;

        foreach (var member in missionResult.Value.AssignedEmployees)
        {
            if ((member.MissionRole == MissionRole.Leader
                ||
                member.MissionRole == MissionRole.CoLeader)
                &&
                member.EmployeeId == employeeIdResult.Value
            )
            {
                isRequesterValid = true;
                break;
            }
        }

        if (!isRequesterValid)
        {
            return Result.Fail(ApplicationError.Forbidden("The employee is not a leader nor a co-leader"));
        }

        // Check if the added member exists
        var memberIdResult = EmployeeId.FromString(memberId);
        if (memberIdResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation("Invalid added member id"));
        }

        var memberResult = await _employeeRepository.GetEmployeeByIdAsync(memberIdResult.Value);
        if (memberResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Internal);
        }

        if (memberResult.Value is null)
        {
            return Result.Fail(ApplicationError.NotFound("The added member is not found"));
        }

        // Check if the added member is already part of the mission
        foreach (var member in missionResult.Value.AssignedEmployees)
        {
            if (member.EmployeeId == memberIdResult.Value)
            {
                return Result.Fail(ApplicationError.Duplicated("This member is already in the project"));
            }
        }

        var result = missionResult.Value.AddMember(memberIdResult.Value, MissionRole.Member);
        if (result.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation(result.Errors[0].Message));    
        }

        result = _missionRepository.UpdateMission(missionResult.Value);
        if (result.IsFailed)
        {
            return Result.Fail(ApplicationError.Internal);    
        }

        await _unitOfWork.SaveChangesAsync();

        return Result.Ok();
    }

    public async Task<Result> DeleteMemberFromMission(string employeeId, string missionId, string memberId)
    {
        if (employeeId == memberId)
        {
            return Result.Fail(ApplicationError.Validation("You cannot delete yourself"));    
        }

        var missionIdResult = MissionId.FromString(missionId);
        if (missionIdResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation("Invalid mission Id"));
        }

        var employeeIdResult = EmployeeId.FromString(employeeId);
        if (employeeIdResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation("Invalid employee Id"));
        }

        var missionResult = await _missionRepository.GetMissionByIdAsync(missionIdResult.Value);
        if (missionResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Internal);
        }
        if (missionResult.Value is null)
        {
            return Result.Fail(ApplicationError.NotFound("Mission is not found"));
        }

        // Check if the requester is valid
        var isRequesterValid = false;

        foreach (var member in missionResult.Value.AssignedEmployees)
        {
            if ((member.MissionRole == MissionRole.Leader
                ||
                member.MissionRole == MissionRole.CoLeader)
                &&
                member.EmployeeId == employeeIdResult.Value
            )
            {
                isRequesterValid = true;
                break;
            }
        }

        if (!isRequesterValid)
        {
            return Result.Fail(ApplicationError.Forbidden("The employee is not a leader nor a co-leader"));
        }

        var memberIdResult = EmployeeId.FromString(memberId);
        if (memberIdResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation("Invalid member Id"));
        }

        var result = missionResult.Value.DeleteMember(memberIdResult.Value);
        if (result.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation(result.Errors[0].Message));    
        }

        result = _missionRepository.UpdateMission(missionResult.Value);
        if (result.IsFailed)
        {
            return Result.Fail(ApplicationError.Internal);
        }

        await _unitOfWork.SaveChangesAsync();

        return Result.Ok();
    }

    public async Task<Result> ChangeMemberRole(string employeeId, string missionId, string memberId, string missionRole)
    {
        if (employeeId == memberId)
        {
            return Result.Fail(ApplicationError.Validation("You cannot change your own role yourself"));    
        }

        var missionIdResult = MissionId.FromString(missionId);
        if (missionIdResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation("Invalid mission Id"));
        }

        var employeeIdResult = EmployeeId.FromString(employeeId);
        if (employeeIdResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation("Invalid employee Id"));
        }

        var missionResult = await _missionRepository.GetMissionByIdAsync(missionIdResult.Value);
        if (missionResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Internal);
        }
        if (missionResult.Value is null)
        {
            return Result.Fail(ApplicationError.NotFound("Mission is not found"));
        }

        // Check if the requester is valid
        var isRequesterValid = false;

        foreach (var member in missionResult.Value.AssignedEmployees)
        {
            if (member.MissionRole == MissionRole.Leader
                &&
                member.EmployeeId == employeeIdResult.Value
            )
            {
                isRequesterValid = true;
                break;
            }
        }

        if (!isRequesterValid)
        {
            return Result.Fail(ApplicationError.Forbidden("The employee is not a leader"));
        }

        var memberIdResult = EmployeeId.FromString(memberId);
        if (memberIdResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation("Invalid member Id"));
        }

        var targetRoleResult = MissionRole.FromString(missionRole);
        if (targetRoleResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation("Invalid mission role"));
        }

        var result = missionResult.Value.ChangeMemberRole(memberIdResult.Value, targetRoleResult.Value);
        if (result.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation(result.Errors[0].Message));
        }

        result = _missionRepository.UpdateMission(missionResult.Value);
        if (result.IsFailed)
        {
            return Result.Fail(ApplicationError.Internal);
        }

        await _unitOfWork.SaveChangesAsync();

        return Result.Ok();
    }

    public async Task<Result<IEnumerable<AssignedEmployeeDto>>> GetMissionMembers(string missionId)
    {
        var missionIdResult = MissionId.FromString(missionId);
        if (missionIdResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation("Invalid mission Id"));
        }

        var missionResult = await _missionRepository.GetMissionByIdAsync(missionIdResult.Value);
        if (missionResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Internal);
        }
        if (missionResult.Value is null)
        {
            return Result.Fail(ApplicationError.NotFound("The mission does not exist"));
        }

        // temporary method
        Dictionary<EmployeeId, MissionRole> employeeRoleDict = [];
        foreach (var assignedEmployee in missionResult.Value.AssignedEmployees)
        {
            employeeRoleDict.Add(assignedEmployee.EmployeeId, assignedEmployee.MissionRole);
        }

        var employeesResult = await _employeeRepository.GetEmployeesByIdsAsync(missionResult.Value.AssignedEmployees.Select(emp => emp.EmployeeId));
        if (employeesResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Internal);
        }

        List<AssignedEmployeeDto> assignedEmployees = [];
        foreach (var employee in employeesResult.Value)
        {
            var missionRole = employeeRoleDict[employee.Id];
            assignedEmployees.Add(EmployeeToAssignedEmployeeDto(employee, missionRole));
        }

        return assignedEmployees;
    }

    private static AssignedEmployeeDto EmployeeToAssignedEmployeeDto(Employee employee, MissionRole missionRole)
    {
        return new AssignedEmployeeDto(employee.Id.ToString(),
                                        employee.FirstName,
                                        employee.LastName,
                                        employee.ImgUrl,
                                        missionRole.ToString());
    }

    private static MissionDto MapToMissionDto(MissionBase mission)
    {
        return new MissionDto(
                        mission.Id.ToString(),
                        mission.Name,
                        mission.Description,
                        mission.Category.ToString(),
                        mission.Status.ToString(),
                        mission.FinishedAt,
                        mission.ResourceLink,
                        mission.CreatedAt,
                        mission.UpdatedAt
                    );
    }
    
    private static Result<MissionDetailDto> MapToMissionDetailDto(MissionBase mission, Employee leader, RoutePlanningSummaryDto? routePlanningSummary = null)
    {
        return new MissionDetailDto(mission.Id.ToString(),
                                    mission.Name,
                                    mission.Description,
                                    mission.Category.ToString(),
                                    mission.Status.ToString(),
                                    new(leader.Id.ToString(),
                                        leader.FirstName,
                                        leader.LastName,
                                        leader.ImgUrl,
                                        MissionRole.Leader.ToString()),
                                    mission.FinishedAt,
                                    mission.ResourceLink,
                                    mission.CreatedAt,
                                    mission.UpdatedAt,
                                    mission.AssignedEmployees.Count,
                                    routePlanningSummary);
    }
}

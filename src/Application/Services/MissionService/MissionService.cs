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

namespace Application.Services.MissionService;

public class MissionService : BaseService, IMissionService
{
    private readonly IMissionRepository _missionRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IRgvRoutePlanning _rgvRoutePlanning;

    public MissionService(IMissionRepository missionRepository,
                          IEmployeeRepository employeeRepository,
                          IRgvRoutePlanning rgvRoutePlanning,
                          IUnitOfWork unitOfWork)
    : base(unitOfWork)
    {
        _missionRepository = missionRepository;
        _employeeRepository = employeeRepository;
        _rgvRoutePlanning = rgvRoutePlanning;
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
            return Result.Fail(ApplicationError.Validation("Invalid employee id"));
        }

        var existing = await _employeeRepository.GetEmployeeByIdAsync(employeeIdResult.Value);
        if (existing is null)
        {
            return Result.Fail(ApplicationError.Validation("Non employee can't create a mission"));
        }

        var missionDomainResult = MissionFactory.CreateMission(employeeId,
                                                               name,
                                                               category,
                                                               description,
                                                               finishedAt);

        if (missionDomainResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation(missionDomainResult.Errors[0].Message));
        }

        var addMissionResult = await _missionRepository.AddMissionAsync(missionDomainResult.Value);

        if (addMissionResult.IsFailed)
        {
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
            return Result.Fail<PagedResult<MissionDto>>(ApplicationError.Internal);
        }

        var getMissionsResult = await _missionRepository.GetAllMissionsAsync(page, pageSize);
        if (getMissionsResult.IsFailed)
        {
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
            return Result.Fail<MissionDetailDto>(ApplicationError.Validation(missionIdResult.Errors[0].Message));
        }

        var getMissionResult = await _missionRepository.GetMissionByIdAsync(missionIdResult.Value);

        if (getMissionResult.IsFailed)
        {
            return Result.Fail<MissionDetailDto>(ApplicationError.Internal);
        }

        if (getMissionResult.Value is null)
        {
            return Result.Fail<MissionDetailDto>(ApplicationError.NotFound("The mission is not found"));
        }

        // get the leader info
        var leader = getMissionResult.Value.AssignedEmployees.FirstOrDefault(ae => ae.MissionRole == MissionRole.Leader);
        if (leader is null) {
            return Result.Fail<MissionDetailDto>(ApplicationError.NotFound("The mission has no leader"));
        }

        var leaderResult = await _employeeRepository.GetEmployeeByIdAsync(leader.EmployeeId);
        if (leaderResult.IsFailed)
        {
            return Result.Fail<MissionDetailDto>(ApplicationError.Internal);
        }
        if (leaderResult.Value is null)
        {
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
            return Result.Fail(ApplicationError.Validation("Invalid mission id"));
        }

        var missionRepoResult = await _missionRepository.GetMissionByIdAsync(missionIdResult.Value);
        if (missionRepoResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Internal);
        }

        if (missionRepoResult.Value is null)
        {
            return Result.Fail(ApplicationError.NotFound("The specified mission is not found"));
        }

        var statusResult = MissionStatus.FromString(updateMissionDto.Status);
        if (statusResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Validation("Invalid mission status"));
        }

        missionRepoResult.Value.SetMissionName(updateMissionDto.Name);
        missionRepoResult.Value.SetMissionDescription(updateMissionDto.Description);
        missionRepoResult.Value.SetMissionStatus(statusResult.Value);

        var updateMissionResult = _missionRepository.UpdateMission(missionRepoResult.Value);
        if (updateMissionResult.IsFailed)
        {
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
            return Result.Fail(ApplicationError.Validation("Invalid missionId"));
        }

        var missionResult = await _missionRepository.GetMissionByIdAsync(missionIdResult.Value);
        if (missionResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Internal);
        }

        if (missionResult.Value is null)
        {
            return Result.Ok();
        }

        var deleteResult = _missionRepository.DeleteMission(missionResult.Value);
        if (deleteResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Internal);
        }

        await _unitOfWork.SaveChangesAsync();

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

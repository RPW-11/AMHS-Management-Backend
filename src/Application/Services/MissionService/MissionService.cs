using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.Mission;
using Application.DTOs.Mission.RoutePlanning;
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
        if (employeeIdResult.IsFailed) {
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

    public async Task<Result<IEnumerable<MissionDto>>> GetAllMission()
    {
        var getMissionsResult = await _missionRepository.GetAllMissionsAsync();

        if (getMissionsResult.IsFailed)
        {
            return Result.Fail<IEnumerable<MissionDto>>(ApplicationError.Internal);
        }

        List<MissionDto> missionsDto = [];
        foreach (var mission in getMissionsResult.Value)
        {
            missionsDto.Add(MapToMissionDto(mission));
        }

        await _unitOfWork.SaveChangesAsync();

        return missionsDto;
    }

    public async Task<Result<MissionDetailDto>> GetMission(string id)
    {
        var missionIdResult = MissionId.FromString(id);
        if (missionIdResult.IsFailed)
        {
            return Result.Fail<MissionDetailDto>(ApplicationError.Validation(missionIdResult.Errors[0].Message));
        }

        var getMissionResult = await _missionRepository.GetMissionDetailedByIdAsync(missionIdResult.Value);

        if (getMissionResult.IsFailed)
        {
            return Result.Fail<MissionDetailDto>(ApplicationError.Internal);
        }

        if (getMissionResult.Value is null)
        {
            return Result.Fail<MissionDetailDto>(ApplicationError.NotFound("The mission is not found"));
        }

        if (getMissionResult.Value.Category == MissionCategory.RoutePlanning.ToString()
            && getMissionResult.Value.ResourceLink is not null)
        {
            RoutePlanningSummaryDto routePlanningSummary = _rgvRoutePlanning.ReadFromJson(getMissionResult.Value.ResourceLink);

            // for now, read the image result and send it as base 64
            byte[] imageBytes = File.ReadAllBytes(routePlanningSummary.ImageUrl);
            string base64String = Convert.ToBase64String(imageBytes);

            routePlanningSummary = new RoutePlanningSummaryDto(routePlanningSummary.Algorithm, base64String, routePlanningSummary.Score);

            getMissionResult.Value.RoutePlanningSummary = routePlanningSummary;

            return getMissionResult.Value;
        }

        await _unitOfWork.SaveChangesAsync();

        return getMissionResult.Value;
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

        missionResult.Value.AddEmployee(memberIdResult.Value, MissionRole.Member);

        _missionRepository.UpdateMission(missionResult.Value);

        await _unitOfWork.SaveChangesAsync();

        return Result.Ok();
    }

    public Task<Result> DeleteMemberFromMission(string employeeId, string missionId, string memberId)
    {
        throw new NotImplementedException();
    }

    public Task<Result> ChangeMemberRole(string employeeId, string missionId, string memberId, string missionRole)
    {
        throw new NotImplementedException();
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
}

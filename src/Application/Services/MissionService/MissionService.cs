using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Persistence;
using Application.DTOs.Mission;
using Domain.Mission;
using Domain.Mission.ValueObjects;
using FluentResults;

namespace Application.Services.MissionService;

public class MissionService : BaseService, IMissionService
{
    private readonly IMissionRepository _missionRepository;

    public MissionService(IMissionRepository missionRepository, IUnitOfWork unitOfWork)
    : base(unitOfWork)
    {
        _missionRepository = missionRepository;
    }

    public async Task<Result> AddMission(string employeeId,
                                         string name,
                                         string category,
                                         string description,
                                         DateTime finishedAt)
    {
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

        return Result.Ok();
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

    public async Task<Result<MissionDto>> GetMission(string id)
    {
        var missionIdResult = MissionId.FromString(id);
        if (missionIdResult.IsFailed)
        {
            return Result.Fail<MissionDto>(ApplicationError.Validation(missionIdResult.Errors[0].Message));
        }

        var getMissionResult = await _missionRepository.GetMissionByIdAsync(missionIdResult.Value);

        if (getMissionResult.IsFailed)
        {
            return Result.Fail<MissionDto>(ApplicationError.Internal);
        }

        if (getMissionResult.Value is null)
        {
            return Result.Fail<MissionDto>(ApplicationError.NotFound("The mission is not found"));
        }

        await _unitOfWork.SaveChangesAsync();

        return MapToMissionDto(getMissionResult.Value);
    }

    private static MissionDto MapToMissionDto(MissionBase mission)
    {
        var assginedEmployees = mission.AssignedEmployees.Count == 0 ? null : mission.AssignedEmployees.Select(x => new AssignedEmployeeDto(x.EmployeeId.ToString(), x.MissionRole.ToString()));
        
        return new MissionDto(
                        mission.Id.ToString(),
                        mission.Name,
                        mission.Description,
                        mission.Category.ToString(),
                        mission.Status.ToString(),
                        mission.FinishedAt,
                        mission.ResourceLink,
                        mission.CreatedAt,
                        mission.UpdatedAt,
                        assginedEmployees
                    );
    }
}

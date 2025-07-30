using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Persistence;
using Application.DTOs.Mission;
using Domain.Entities;
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

    public async Task<Result> AddMission(string name, string category, string description)
    {
        var missionDomainResult = Mission.Create(name, category, description);

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
        if (!Guid.TryParse(id, out var missionGuid))
        {
            return Result.Fail<MissionDto>(ApplicationError.Validation("Invalid mission id"));
        }
        var getMissionResult = await _missionRepository.GetMissionByIdAsync(missionGuid);

        if (getMissionResult.IsFailed)
        {
            return Result.Fail<MissionDto>(ApplicationError.Internal);
        }

        if (getMissionResult.Value == null)
        {
            return Result.Fail<MissionDto>(ApplicationError.NotFound("The mission is not found"));
        }

        await _unitOfWork.SaveChangesAsync();

        return MapToMissionDto(getMissionResult.Value);
    }

    private static MissionDto MapToMissionDto(Mission mission)
    {
        return new MissionDto(
                        mission.Id,
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

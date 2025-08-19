using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.Mission;
using Application.DTOs.Mission.RoutePlanning;
using Domain.Mission;
using Domain.Mission.ValueObjects;
using FluentResults;

namespace Application.Services.MissionService;

public class MissionService : BaseService, IMissionService
{
    private readonly IMissionRepository _missionRepository;
    private readonly IRgvRoutePlanning _rgvRoutePlanning;

    public MissionService(IMissionRepository missionRepository,
                          IRgvRoutePlanning rgvRoutePlanning,
                          IUnitOfWork unitOfWork)
    : base(unitOfWork)
    {
        _missionRepository = missionRepository;
        _rgvRoutePlanning = rgvRoutePlanning;
    }

    public async Task<Result<AddMissionDto>> AddMission(string employeeId,
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

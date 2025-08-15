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

        if (getMissionResult.Value.Category == MissionCategory.RoutePlanning && getMissionResult.Value.ResourceLink is not null)
        {
            RoutePlanningSummaryDto routePlanningSummary = _rgvRoutePlanning.ReadFromJson(getMissionResult.Value.ResourceLink);

            // for now, read the image result and send it as base 64
            byte[] imageBytes = File.ReadAllBytes(routePlanningSummary.ImageUrl);
            string base64String = Convert.ToBase64String(imageBytes);

            routePlanningSummary = new RoutePlanningSummaryDto(routePlanningSummary.Algorithm, base64String);

            return MapToMissionDto(getMissionResult.Value, routePlanningSummary);
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
                        assginedEmployees,
                        null
                    );
    }
    
    private static MissionDto MapToMissionDto(MissionBase mission, RoutePlanningSummaryDto routePlanningSummary)
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
                        assginedEmployees,
                        routePlanningSummary
                    );
    }
}

using Application.DTOs.Mission;
using FluentResults;

namespace Application.Services.MissionService;

public interface IMissionService
{
    Task<Result<AddMissionDto>> AddMission(string employeeId,
                                           string name,
                                           string category,
                                           string description,
                                           DateTime finishedAt);
    Task<Result<MissionDetailDto>> GetMission(string id);
    Task<Result<IEnumerable<MissionDto>>> GetAllMission();
    Task<Result> UpdateMission(UpdateMissionDto updateMissionDto, string missionId);
    Task<Result> DeleteMission(string missionId);
}

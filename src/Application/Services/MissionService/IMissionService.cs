using Application.DTOs.Common;
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
    Task<Result<PagedResult<MissionDto>>> GetAllMission(
        int page,
        int pageSize,
        string? searchTerm = null
    );
    Task<Result<PagedResult<MissionDto>>> GetAllMissionsByEmployeeId(
        string employeeId,
        int page, 
        int pageSize, 
        string? searchTerm
    );
    Task<Result> UpdateMission(UpdateMissionDto updateMissionDto, string missionId);
    Task<Result> DeleteMission(string missionId);
    Task<Result> DeleteMissions(IEnumerable<string> missionIds);
    Task<Result> AddMemberToMission(string employeeId, string missionId, string memberId);
    Task<Result> DeleteMemberFromMission(string employeeId, string missionId, string memberId);
    Task<Result> ChangeMemberRole(string employeeId, string missionId, string memberId, string missionRole);
    Task<Result<IEnumerable<AssignedEmployeeDto>>> GetMissionMembers(string missionId);
}

using Domain.Employee.ValueObjects;
using Domain.Mission;
using Domain.Mission.ValueObjects;
using FluentResults;

namespace Application.Common.Interfaces.Persistence;

public interface IMissionRepository
{
    Task<Result<IEnumerable<MissionBase>>> GetAllMissionsAsync(int page, int pageSize);
    Task<Result<IEnumerable<MissionBase>>>GetAllMissionsByUserIdAsync(EmployeeId employeeId, int page, int pageSize);
    Task<Result<int>> GetMissionsCountAsync(EmployeeId? employeeId = null);
    Task<Result<MissionBase?>> GetMissionByIdAsync(MissionId id);
    Task<Result> AddMissionAsync(MissionBase mission);
    Result UpdateMission(MissionBase mission);
    Result DeleteMission(MissionBase mission);
    Task<Result<int>> DeleteMissionsAsync(IEnumerable<MissionId> missionIds);
}

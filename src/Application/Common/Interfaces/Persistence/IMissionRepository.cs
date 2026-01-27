using Application.DTOs.Common;
using Application.DTOs.Mission;
using Domain.Missions;
using Domain.Missions.ValueObjects;
using FluentResults;

namespace Application.Common.Interfaces.Persistence;

public interface IMissionRepository
{
    Task<Result<PagedResult<MissionBase>>> GetAllMissionsAsync(MissionFilterDto missionFilterDto);
    Task<Result<MissionBase?>> GetMissionByIdAsync(MissionId id);
    Task<Result> AddMissionAsync(MissionBase mission);
    Result UpdateMission(MissionBase mission);
    Result DeleteMission(MissionBase mission);
    Task<Result<int>> DeleteMissionsAsync(IEnumerable<MissionId> missionIds);
}

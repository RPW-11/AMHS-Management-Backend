using Application.DTOs.Mission;
using Domain.Mission;
using Domain.Mission.ValueObjects;
using FluentResults;

namespace Application.Common.Interfaces.Persistence;

public interface IMissionRepository
{
    Task<Result<IEnumerable<MissionBase>>> GetAllMissionsAsync();
    Task<Result<MissionBase?>> GetMissionByIdAsync(MissionId id);

    Task<Result<MissionDetailDto?>> GetMissionDetailedByIdAsync(MissionId id);
    Task<Result> AddMissionAsync(MissionBase mission);
    Result UpdateMission(MissionBase mission);
    Result DeleteMission(MissionBase mission);
}

using Application.DTOs.Mission;
using FluentResults;

namespace Application.Services.MissionService;

public interface IMissionService
{
    Task<Result> AddMission(string name, string category, string description);
    Task<Result<MissionDto>> GetMission(string id);
    Task<Result<IEnumerable<MissionDto>>> GetAllMission();
}

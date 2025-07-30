using Domain.Entities;
using FluentResults;

namespace Application.Common.Interfaces.Persistence;

public interface IMissionRepository
{
    Task<Result<IEnumerable<Mission>>> GetAllMissionsAsync();
    Task<Result<Mission?>> GetMissionByIdAsync(Guid id);
    Task<Result> AddMissionAsync(Mission mission);
}

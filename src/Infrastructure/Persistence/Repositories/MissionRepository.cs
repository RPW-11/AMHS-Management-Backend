using Application.Common.Interfaces.Persistence;
using Domain.Mission;
using Domain.Mission.ValueObjects;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class MissionRepository : IMissionRepository
{
    private readonly AppDbContext _dbContext;

    public MissionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> AddMissionAsync(MissionBase mission)
    {
        try
        {
            await _dbContext.AddAsync(mission);
            return Result.Ok();
        }
        catch (Exception erorr)
        {
            return Result.Fail(new Error("Fail to add a mission to the database").CausedBy(erorr));
        }
    }

    public async Task<Result<IEnumerable<MissionBase>>> GetAllMissionsAsync()
    {
        try
        {
            var missionsResult = await _dbContext.Missions.ToListAsync();

            return missionsResult;
        }
        catch (Exception erorr)
        {
            return Result.Fail(new Error("Fail to get all missions from the database").CausedBy(erorr));
        }
    }

    public async Task<Result<MissionBase?>> GetMissionByIdAsync(MissionId id)
    {
        try
        {
            var missionResult = await _dbContext.Missions
                                        .Include(m => m.AssignedEmployees)
                                        .FirstOrDefaultAsync(m => m.Id == id);

            return missionResult;
        }
        catch (Exception erorr)
        {
            return Result.Fail(new Error("Fail to get the mission from the database").CausedBy(erorr));
        }
    }

    public Result UpdateMission(MissionBase mission)
    {
        try
        {
            _dbContext.Update(mission);
            return Result.Ok();
        }
        catch (Exception erorr)
        {
            return Result.Fail(new Error("Fail to update the mission").CausedBy(erorr));
        }
    }
}

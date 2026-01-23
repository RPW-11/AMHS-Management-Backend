using Application.Common.Interfaces.Persistence;
using Application.DTOs.Common;
using Application.DTOs.Mission;
using Domain.Employee.ValueObjects;
using Domain.Mission;
using Domain.Mission.ValueObjects;
using FluentResults;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
        catch (Exception error)
        {
            Console.WriteLine(error);
            return Result.Fail(new Error("Fail to add a mission to the database").CausedBy(error));
        }
    }

    public async Task<Result<PagedResult<MissionBase>>> GetAllMissionsAsync(MissionFilterDto missionFilterDto)
    {
        try
        {
            IQueryable<MissionBase> query = _dbContext.Missions;

            if (missionFilterDto.EmployeeId is not null)
                query = query.Where(m => m.AssignedEmployees.Any(ae => ae.EmployeeId == missionFilterDto.EmployeeId));

            if (missionFilterDto.Status is not null)
                query = query.Where(m => m.Status == missionFilterDto.Status);

            if (!string.IsNullOrWhiteSpace(missionFilterDto.Name))
                query = query.Where(m => EF.Functions.ILike(m.Name, $"%{missionFilterDto.Name.Trim()}%"));

            int totalCount = await query.CountAsync();
            
            int page = missionFilterDto.Page;
            int pageSize = missionFilterDto.PageSize;

            var items = await query.OrderByDescending(m => m.UpdatedAt)
                                    .Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();


            return new PagedResult<MissionBase>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            return Result.Fail(new Error("Fail to get all missions from the database").CausedBy(error));
        }
    }

    public async Task<Result<MissionBase?>> GetMissionByIdAsync(MissionId id)
    {
        try
        {
            MissionBase? mission = await _dbContext.Missions
                                    .Include(m => m.AssignedEmployees)
                                    .FirstOrDefaultAsync(e => e.Id == id);
            return mission;
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            return Result.Fail(new Error("Fail to get the mission from the database").CausedBy(error));

        }
    }

    public Result UpdateMission(MissionBase mission)
    {
        try
        {
            _dbContext.Update(mission);
            return Result.Ok();
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            return Result.Fail(new Error("Fail to update the mission").CausedBy(error));
        }
    }

    public Result DeleteMission(MissionBase mission)
    {
        try
        {
            _dbContext.Attach(mission);
            _dbContext.Remove(mission);

            return Result.Ok();
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            return Result.Fail(new Error("Fail to update the mission").CausedBy(error));
        }
    }

    public async Task<Result<int>> GetMissionsCountAsync(EmployeeId? employeeId)
    {
        try
        {
            if (employeeId is null)
            {
                var count = await _dbContext.Missions.CountAsync();
                return count;
            }
            else
            {
                var count = await _dbContext.Missions.Where(m => m.AssignedEmployees.Any(e => e.EmployeeId == employeeId)).CountAsync();
                return count;
            }
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            return Result.Fail(new Error("Fail to count the number of missions").CausedBy(error));
        }
    }

    public async Task<Result<IEnumerable<MissionBase>>> GetAllMissionsByUserIdAsync(EmployeeId employeeId, int page, int pageSize)
    {
        try
        {
            var missionsResult = await _dbContext.Missions
                                    .Where(m => m.AssignedEmployees.Any(e => e.EmployeeId == employeeId))
                                    .OrderByDescending(m => m.UpdatedAt)
                                    .Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();

            return missionsResult;
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            return Result.Fail(new Error("Fail to get all missions from the database").CausedBy(error));
        }
    }

    public async Task<Result<int>> DeleteMissionsAsync(IEnumerable<MissionId> missionIds)
    {
        try
        {
            var deletedRows = await _dbContext.Missions.Where(m => missionIds.Contains(m.Id)).ExecuteDeleteAsync();
            return deletedRows;
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            return Result.Fail(new Error("Fail to delete missions").CausedBy(error));
        }
    }
}

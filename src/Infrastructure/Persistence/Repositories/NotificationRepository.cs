using Application.Common.Interfaces.Persistence;
using Application.DTOs.Common;
using Application.DTOs.Notification;
using Domain.Employees.ValueObjects;
using Domain.Notifications;
using Domain.Notifications.ValueObjects;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _dbContext;
    public NotificationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<Result> AddNotificationAsync(Notification notification)
    {
        try
        {
            await _dbContext.Notifications.AddAsync(notification);
            return Result.Ok();
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            return Result.Fail(new Error("Fail to insert the employee to the database").CausedBy(error));
        }
    }

    public async Task<Result> AddNotificationsAsync(IEnumerable<Notification> notifications)
    {
        try
        {
            await _dbContext.Notifications.AddRangeAsync(notifications);
            return Result.Ok();
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            return Result.Fail(new Error("Fail to insert the notifications to the database").CausedBy(error));
        }
    }

    public async Task<Result> DeleteNotificationAsync(NotificationId notificationId)
    {
        try
        {
            await _dbContext.Notifications.Where(n => n.Id == notificationId).ExecuteDeleteAsync();
            return Result.Ok();
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            return Result.Fail(new Error("Fail to delete the notification from the database").CausedBy(error));
        }
    }

    public async Task<Result<Notification?>> GetNotificationByIdAsync(NotificationId notificationId)
    {
        try
        {
            var notification = await _dbContext.Notifications.FirstAsync(n => n.Id == notificationId);
            return notification;
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            return Result.Fail(new Error("Fail to get the notification from the database").CausedBy(error));
        }
    }

    public async Task<Result<PagedResult<Notification>>> GetNotificationsByEmployeeIdAsync(NotificationFilterDto notificationFilterDto)
    {
        try
        {
            IQueryable<Notification> query = _dbContext.Notifications;

            if (notificationFilterDto.IsRead is not null)
            {
                query = query.Where(m => (bool)notificationFilterDto.IsRead ? m.ReadAt != null : m.ReadAt == null);
            }

            int totalCount = await query.CountAsync();

            int page = notificationFilterDto.Page;
            int pageSize = notificationFilterDto.PageSize;

            var items = await query.OrderByDescending(m => m.CreatedAt)
                                    .Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();

            return new PagedResult<Notification>
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
            return Result.Fail(new Error("Fail to get the notification from the database").CausedBy(error));
        }
    }

    public Result UpdateNotificationAsync(Notification notification)
    {
        try
        {
            _dbContext.Notifications.Update(notification);
            return Result.Ok();
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            return Result.Fail(new Error("Fail to update the notification").CausedBy(error));
        }
    }
}
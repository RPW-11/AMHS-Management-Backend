using Domain.Employees.ValueObjects;
using FluentResults;

namespace Application.DTOs.Notification;


public class NotificationFilterDto
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public EmployeeId EmployeeId { get; set; }
    public bool? IsRead { get; set; }

    private NotificationFilterDto(int page, int pageSize, EmployeeId employeeId, bool? isRead)
    {
        Page = page;
        PageSize = pageSize;
        EmployeeId = employeeId;
        IsRead = isRead;
    }

    public static Result<NotificationFilterDto> Create(int page, int pageSize, string employeeId, string? type)
    {
        var employeeIdResult = EmployeeId.FromString(employeeId);
        if (employeeIdResult.IsFailed)
        {
            return Result.Fail<NotificationFilterDto>("Invalid employee id");
        }

        var empId = employeeIdResult.Value;

        bool? isRead = null;
        if (!string.IsNullOrWhiteSpace(type))
        {   
            if (type.Equals("unread", StringComparison.CurrentCultureIgnoreCase))
            {
                isRead = false;
            }
            else if(type.Equals("read", StringComparison.CurrentCultureIgnoreCase)) 
            {
                isRead = true;
            }
            else if (!type.Equals("all", StringComparison.CurrentCultureIgnoreCase))
            {
                return Result.Fail<NotificationFilterDto>("Invalid query type");
            }
        } 

        return new NotificationFilterDto(page, pageSize, empId, isRead);
    }
}
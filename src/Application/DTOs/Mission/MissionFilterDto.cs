using Domain.Employees.ValueObjects;
using Domain.Missions.ValueObjects;
using FluentResults;

namespace Application.DTOs.Mission;


public class MissionFilterDto
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public EmployeeId? EmployeeId { get; set; }
    public MissionStatus? Status { get; set; }
    public string? Name { get; set; }

    private MissionFilterDto(int page, int pageSize, EmployeeId? employeeId, MissionStatus? status, string? name)
    {
        Page = page;
        PageSize = pageSize;
        EmployeeId = employeeId;
        Status = status;
        Name = name;
    }

    public static Result<MissionFilterDto> Create(int page, int pageSize, string? employeeId, string? status, string? name)
    {
        EmployeeId? empId = null;
        MissionStatus? missionStatus = null;
        if (!string.IsNullOrWhiteSpace(employeeId))
        {
            var employeeIdResult = EmployeeId.FromString(employeeId);
            if (employeeIdResult.IsFailed)
            {
                return Result.Fail<MissionFilterDto>("Invalid employee id");
            }
            empId = employeeIdResult.Value;
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var missionStatusResult = MissionStatus.FromString(status);
            if (missionStatusResult.IsFailed)
            {
                return Result.Fail<MissionFilterDto>("Invalid mission status");
            }
            missionStatus = missionStatusResult.Value;
        }

        return new MissionFilterDto(page, pageSize, empId, missionStatus, name);
    }
}
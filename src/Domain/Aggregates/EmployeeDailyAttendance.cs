using Domain.Errors.EmployeeAttendance;
using Domain.Events;
using Domain.ValueObjects;
using FluentResults;

namespace Domain.Aggregates;

public class EmployeeDailyAttendance : AggregateRoot<Guid>
{
    public Guid EmployeeId { get; private set; }
    public DateTime Date { get; private set; }
    public TimeSpan? CheckInTime { get; private set; }
    public TimeSpan? CheckOutTime { get; private set; }
    public List<EmployeeBreakPeriod> Breaks { get; private set; }

    public EmployeeAttendanceStatus Status { get; private set; }

    private EmployeeDailyAttendance() { }

    public EmployeeDailyAttendance(Guid employeeId, DateTime date)
    {
        EmployeeId = employeeId;
        Date = date;
        Status = EmployeeAttendanceStatus.Absent;
    }

    public Result CheckIn(DateTime timestamp)
    {
        if (CheckInTime.HasValue)
        {
            return Result.Fail(new AlreadyCheckedInError());
        }

        var checkInTime = timestamp.TimeOfDay;

        if (checkInTime > TimeSpan.FromHours(8))
        {
            Status = EmployeeAttendanceStatus.Late;
        }
        else
        {
            Status = EmployeeAttendanceStatus.Present;
        }

        CheckInTime = checkInTime;
        AddDomainEvent(new EmployeeCheckedInEvent(this.EmployeeId));

        return Result.Ok();
    }

    public Result CheckOut(DateTime timestamp)
    {
        if (!CheckInTime.HasValue)
        {
            return Result.Fail(new MustCheckInFirstError());
        }
        if (CheckOutTime.HasValue)
        {
            return Result.Fail(new AlreadyCheckedOutError());
        }

        var checkedOutTime = timestamp.TimeOfDay;

        if (checkedOutTime < TimeSpan.FromHours(17))
        {
            if (!IsWithinBreakPeriod(checkedOutTime))
            {
                return Result.Fail(new NotWithinBreakCheckOutError());
            }

            Breaks.Add(new EmployeeBreakPeriod(checkedOutTime, null));
        }

        CheckOutTime = checkedOutTime;
        AddDomainEvent(new EmployeeCheckedOutEvent(this.EmployeeId));

        return Result.Ok();
    }

    public Result ReturnFromBreak(DateTime timestamp)
    {
        var returnTime = timestamp.TimeOfDay;

        if (!IsWithinBreakPeriod(returnTime))
        {
            return Result.Fail(new NotWithinBreakCheckOutError());
        }

        var lastBreak = Breaks.LastOrDefault();

        if (lastBreak == null || lastBreak.EndTime.HasValue)
        {
            return Result.Fail(new NoActiveBreakException());
        }

        lastBreak.SetEndTime(returnTime);
        AddDomainEvent(new EmployeeReturnFromBreakEvent(this.EmployeeId));

        return Result.Ok();
    }

    private static bool IsWithinBreakPeriod(TimeSpan time)
    {
        return time >= TimeSpan.Parse("11:50") && time <= TimeSpan.Parse("13:00");
    }
}

public enum EmployeeAttendanceStatus
{
    Absent, Present, Late
}

using Domain.Common;
using Domain.Events;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Aggregates;

public class EmployeeDailyAttendance : AggregateRoot<Guid>
{
    public Guid EmployeeId { get; private set; }
    public DateTime Date { get; private set; }

    public TimeSpan? CheckInTime { get; private set; }
    public TimeSpan? CheckOutTime { get; private set; }
    public List<EmployeeBreakPeriod> Breaks { get; private set; } = [];

    public EmployeeAttendanceStatus Status { get; private set; }

    private EmployeeDailyAttendance() { }

    public EmployeeDailyAttendance(Guid employeeId, DateTime date)
    {
        EmployeeId = employeeId;
        Date = date;
        Status = EmployeeAttendanceStatus.Absent;
    }

    public void CheckIn(DateTime timestamp)
    {
        if (CheckInTime.HasValue)
        {
            throw new AlreadyCheckedInException();
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
    }

    public void CheckOut(DateTime timestamp)
    {
        if (!CheckInTime.HasValue)
        {
            throw new MustCheckInFirstException();
        }
        if (CheckOutTime.HasValue)
        {
            throw new AlreadyCheckedOutException();
        }

        var checkedOutTime = timestamp.TimeOfDay;

        if (checkedOutTime < TimeSpan.FromHours(17))
        {
            // check if it is withen the break period
            if (!IsWithinBreakPeriod(checkedOutTime))
            {
                throw new NotWithinBreakCheckOutException();
            }

            Breaks.Add(new EmployeeBreakPeriod(checkedOutTime, null));
        }

        CheckOutTime = checkedOutTime;
        AddDomainEvent(new EmployeeCheckedOutEvent(this.EmployeeId));
    }

    public void ReturnFromBreak(DateTime timestamp)
    {
        var returnTime = timestamp.TimeOfDay;

        if (!IsWithinBreakPeriod(returnTime))
        {
            throw new NotWithinBreakCheckOutException();
        }

        var lastBreak = Breaks.LastOrDefault();

        if (lastBreak == null || lastBreak.EndTime.HasValue)
        {
            throw new NoActiveBreakException();
        }

        lastBreak.SetEndTime(returnTime);
        AddDomainEvent(new EmployeeReturnFromBreakEvent(this.EmployeeId));
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

namespace Domain.Errors.EmployeeAttendance;

public sealed class AlreadyCheckedInError : DomainError
{
    public AlreadyCheckedInError() 
    : base("You already checked-in today", "EmployeeAttendance.AlreadyCheckedIn", "")
    {
    }
}

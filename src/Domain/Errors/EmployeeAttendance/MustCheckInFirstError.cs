namespace Domain.Errors.EmployeeAttendance;

public sealed class MustCheckInFirstError : DomainError
{
    public MustCheckInFirstError() 
    : base("You must check in first", "EmployeeAttendance.MustCheckInFirst", "")
    {
    }
}

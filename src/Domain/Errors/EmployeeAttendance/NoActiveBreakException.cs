namespace Domain.Errors.EmployeeAttendance;

public sealed class NoActiveBreakException : DomainError
{
    public NoActiveBreakException() 
    : base("There are no active breaks", "EmployeeAttendance.NoActiveBreakException", "")
    {
    }
}

namespace Domain.Errors.EmployeeAttendance;

public sealed class AlreadyCheckedOutError : DomainError
{
    public AlreadyCheckedOutError() 
    : base("You already checked out today", "EmployeeAttendance.AlreadyCheckedOut", "")
    {
    }
}

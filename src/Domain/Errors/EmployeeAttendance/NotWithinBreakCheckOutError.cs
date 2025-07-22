namespace Domain.Errors.EmployeeAttendance;

public sealed class NotWithinBreakCheckOutError : DomainError
{
    public NotWithinBreakCheckOutError() 
    : base("Break returns only allowed between 11:50-13:00", "EmployeeAttendance.NotWithinBreakCheckOut", "")
    {
    }
}

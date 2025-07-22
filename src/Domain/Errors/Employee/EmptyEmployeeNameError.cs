namespace Domain.Errors.Employee;

public sealed class EmptyEmployeeNameError : DomainError
{
    public EmptyEmployeeNameError() 
    : base("Employee name can't be empty", "Employee.EmptyEmployeeName", "")
    {
    }
}

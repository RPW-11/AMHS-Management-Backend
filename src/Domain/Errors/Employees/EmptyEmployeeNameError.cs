namespace Domain.Errors.Employees;

public sealed class EmptyEmployeeNameError : DomainError
{
    public EmptyEmployeeNameError() 
    : base("Employee name can't be empty", "Employee.EmptyEmployeeName", "")
    {
    }
}

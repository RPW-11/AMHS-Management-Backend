namespace Domain.Errors.Employees;

public sealed class InvalidEmployeePositionError : DomainError
{
    public InvalidEmployeePositionError(string position) :
    base("Invalid employee position", "Employee.InvalidPosition", $"position '${position}' does not exist")
    {
    }
}

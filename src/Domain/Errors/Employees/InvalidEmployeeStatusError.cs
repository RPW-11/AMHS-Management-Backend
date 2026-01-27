namespace Domain.Errors.Employees;

public class InvalidEmployeeStatusError : DomainError
{
    public InvalidEmployeeStatusError(string status)
     : base("Invalid employee status", "Employee.InvalidStatus", $"The employee status '{status}' does not exist")
    {
    }
}

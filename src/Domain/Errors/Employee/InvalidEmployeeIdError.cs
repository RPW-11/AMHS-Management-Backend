namespace Domain.Errors.Employee;

public class InvalidEmployeeIdError : DomainError
{
    public InvalidEmployeeIdError(string id) 
        : base("Invalid employee id", "Employee.InvalidEmployeeId", $"The {id} is not valid")
    {
    }
}

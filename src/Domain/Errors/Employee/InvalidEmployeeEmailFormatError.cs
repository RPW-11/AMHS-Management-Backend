namespace Domain.Errors.Employee;

public sealed class InvalidEmployeeEmailFormatError : DomainError
{
    public InvalidEmployeeEmailFormatError(string email) 
    : base("Invalid email format", "Employee.InvalidEmail", $"the email '{email}' is incorrect")
    {
    }
}

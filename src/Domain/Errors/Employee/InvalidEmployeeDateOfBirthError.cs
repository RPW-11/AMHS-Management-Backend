namespace Domain.Errors.Employee;

public class InvalidEmployeeDateOfBirthError : DomainError
{
    public InvalidEmployeeDateOfBirthError() : base("Invalid employee date of birth", "Employee.InvalidDateOfBirth", "")
    {
    }
}

namespace Domain.Errors.Employees;

public class InvalidEmployeeDateOfBirthError : DomainError
{
    public InvalidEmployeeDateOfBirthError() : base("Invalid employee date of birth", "Employee.InvalidDateOfBirth", "")
    {
    }
}

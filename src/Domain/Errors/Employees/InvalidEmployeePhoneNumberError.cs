namespace Domain.Errors.Employees;

public class InvalidEmployeePhoneNumberError : DomainError
{
    public InvalidEmployeePhoneNumberError() : base("Invalid employee's phone number", "Employee.InvalidPhoneNumber", "The phone number is either empty or less than 8 characters")
    {
    }
}

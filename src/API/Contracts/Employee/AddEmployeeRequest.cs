namespace API.Contracts.Employee;

public record class AddEmployeeRequest
(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Position,
    string PhoneNumber,
    string DateOfBirth
);

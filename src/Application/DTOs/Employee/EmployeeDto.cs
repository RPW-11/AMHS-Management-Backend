namespace Application.DTOs.Employee;

public record EmployeeDto
(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Position,
    string Age,
    string PhoneNumber,
    DateTime DateOfBirth,
    DateTime JoinDate,
    string Status,
    string? ImgUrl
);

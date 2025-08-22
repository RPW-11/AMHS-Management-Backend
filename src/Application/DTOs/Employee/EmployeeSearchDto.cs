namespace Application.DTOs.Employee;

public record class EmployeeSearchDto
(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    string? ImgUrl
);

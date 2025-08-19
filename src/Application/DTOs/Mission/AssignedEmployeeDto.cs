namespace Application.DTOs.Mission;

public record AssignedEmployeeDto(
    string Id,
    string FirstName,
    string LastName,
    string? ImageUrl,
    string Role
);

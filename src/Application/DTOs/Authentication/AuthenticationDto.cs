namespace Application.DTOs.Authentication;

public record AuthenticationDto
(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Position,
    DateTime DateOfBirth,
    DateTime JoinDate,
    string Status,
    string? ImgUrl,
    string Token
);

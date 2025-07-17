using Domain.Entities;

namespace Application.Services.Authentication;

public record AuthenticationResult
(
    Employee Employee,
    string Token
);

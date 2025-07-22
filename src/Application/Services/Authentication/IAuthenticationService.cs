using Application.DTOs.Authentication;
using FluentResults;

namespace Application.Services.Authentication;

public interface IAuthenticationService
{
    Result<AuthenticationDto> Login(string Email, string Password);
    Result<AuthenticationDto> Register(string FirstName, string LastName, string Email, string Password);
}

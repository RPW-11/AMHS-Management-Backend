using Application.DTOs.Authentication;
using FluentResults;

namespace Application.Services.Authentication;

public interface IAuthenticationService
{
    Task<Result<AuthenticationDto>> Login(string Email, string Password);
    Task<Result<AuthenticationDto>> Register(string FirstName, string LastName, string Email, string Password);
}

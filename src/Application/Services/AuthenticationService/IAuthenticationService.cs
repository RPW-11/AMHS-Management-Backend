using Application.DTOs.Authentication;
using FluentResults;

namespace Application.Services.AuthenticationService;

public interface IAuthenticationService
{
    Task<Result<AuthenticationDto>> Login(string email, string password);
}

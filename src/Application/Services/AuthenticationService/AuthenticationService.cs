using Application.Common.Errors;
using Application.Common.Interfaces.Authentication;
using Application.Common.Interfaces.Persistence;
using Application.DTOs.Authentication;
using Domain.Entities;
using FluentResults;
using Application.Common.Interfaces.Security;

namespace Application.Services.AuthenticationService;

public class AuthenticationService : IAuthenticationService
{
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmployeeRepository _employeeRepository;

    public AuthenticationService(IJwtTokenGenerator jwtTokenGenerator, IPasswordHasher passwordHasher, IEmployeeRepository employeeRepository)
    {
        _jwtTokenGenerator = jwtTokenGenerator;
        _employeeRepository = employeeRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<AuthenticationDto>> Login(string email, string password)
    {
        Employee? employee = await _employeeRepository.GetEmployeeByEmailAsync(email);

        if (employee is null || !_passwordHasher.VerifyPassword(password, employee.HashedPassword))
        {
            return Result.Fail<AuthenticationDto>(new ApplicationError("Incorrect email or password", 400));
        }

        string token = _jwtTokenGenerator.GenerateToken(employee);

        return MapToDto(employee, token);
    }

    private static AuthenticationDto MapToDto(Employee employee, string token)
    {
        return new AuthenticationDto(
            employee.Id,
            employee.FirstName,
            employee.LastName,
            employee.Email,
            employee.Position.ToString(),
            employee.DateOfBirth,
            employee.JoinDate,
            employee.Status.ToString(),
            employee.ImgUrl,
            token
        );
    }
}

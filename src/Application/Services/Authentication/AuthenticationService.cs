using Application.Common.Errors;
using Application.Common.Interfaces.Authentication;
using Application.Common.Interfaces.Persistence;
using Application.DTOs.Authentication;
using Domain.Entities;
using Domain.Errors;
using Domain.ValueObjects;
using FluentResults;

namespace Application.Services.Authentication;

public class AuthenticationService : IAuthenticationService
{
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IEmployeeRepository _employeeRepository;

    public AuthenticationService(IJwtTokenGenerator jwtTokenGenerator, IEmployeeRepository employeeRepository)
    {
        _jwtTokenGenerator = jwtTokenGenerator;
        _employeeRepository = employeeRepository;
    }

    public Result<AuthenticationDto> Login(string email, string password)
    {
        Employee? employee = _employeeRepository.GetEmployeeByEmail(email);

        if (employee is null || employee.HashedPassword != password)
        {
            return Result.Fail<AuthenticationDto>(new ApplicationError("Incorrect email or password", 400));
        }

        string token = _jwtTokenGenerator.GenerateToken(employee);

        return MapToDto(employee, token);
    }

    public Result<AuthenticationDto> Register(string firstName, string lastName, string email, string password)
    {
        if (_employeeRepository.GetEmployeeByEmail(email) is not null)
        {
            return Result.Fail<AuthenticationDto>(new ApplicationError("The email already exists", 409));
        }

        var domainResult = Employee.Create(
            firstName: firstName,
            lastName: lastName,
            email: email,
            hashedPassword: password,
            position: "Staff",
            dateOfBirth: new DateTime(2001, 11, 11),
            status: "Active"
        );

        if (domainResult.IsFailed)
        {
            var error = domainResult.Errors[0];
            if (error is DomainError)
            {
                return Result.Fail<AuthenticationDto>(new ApplicationError(error.Message, 409));
            }
            return Result.Fail<AuthenticationDto>(new ApplicationError("Server error", 500));
        }

        Employee newEmployee = domainResult.Value;

        _employeeRepository.AddEmployee(newEmployee);

        string token = _jwtTokenGenerator.GenerateToken(newEmployee);
        return MapToDto(newEmployee, token);
    }

    private static Result<AuthenticationDto> MapToDto(Employee employee, string token)
    {
        return new AuthenticationDto(
            employee.Id,
            employee.FirstName,
            employee.LastName,
            employee.Email,
            employee.Position.ToStringValue(),
            employee.DateOfBirth,
            employee.JoinDate,
            employee.Status,
            employee.ImgUrl,
            token
        );
    }
}

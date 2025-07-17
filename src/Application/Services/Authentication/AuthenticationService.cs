using Application.Common.Interfaces.Authentication;
using Application.Common.Interfaces.Persistence;
using Domain.Entities;
using Domain.ValueObjects;

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

    public AuthenticationResult Login(string email, string password)
    {
        // Check if the user exists
        Employee? employee = _employeeRepository.GetEmployeeByEmail(email);

        if (employee is null || employee.Password != password)
        {
            throw new Exception("Incorrect email or pass bro");
        }

        // give the token
        string token = _jwtTokenGenerator.GenerateToken(employee);

        return new AuthenticationResult(
            employee,
            token
        );
    }

    public AuthenticationResult Register(string firstName, string lastName, string email, string password)
    {
        // Check if the employee already exists
        if (_employeeRepository.GetEmployeeByEmail(email) is not null)
        {
            throw new Exception("This email is already used bro..");
        }

        // Create the employee
        Employee newEmployee = new Employee(
            firstName: firstName,
            lastName: lastName,
            email: new EmployeeEmail(email),
            password: password,
            position: EmployeePositionExtension.ToEmployeePosition("Staff"),
            dateOfBirth: new DateTime(2001, 11, 11),
            status: "Active"
        );

        _employeeRepository.AddEmployee(newEmployee);

        // Create the token
        string token = _jwtTokenGenerator.GenerateToken(newEmployee);
        
        return new AuthenticationResult(
            newEmployee,
            token
        );
    }
}

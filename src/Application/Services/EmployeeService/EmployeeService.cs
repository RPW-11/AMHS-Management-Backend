using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Security;
using Application.DTOs.Employee;
using Domain.Entities;
using Domain.Errors;
using Domain.ValueObjects.Employee;
using FluentResults;

namespace Application.Services.EmployeeService;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IPasswordHasher _passwordHasher;

    public EmployeeService(IEmployeeRepository employeeRepository, IPasswordHasher passwordHasher)
    {
        _employeeRepository = employeeRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> AddEmployee(string firstName, string lastName, string email, string password, string position, string phoneNumber, string dateOfBirth)
    {
        if (await _employeeRepository.GetEmployeeByEmailAsync(email) is not null)
        {
            return Result.Fail(new ApplicationError("The email already exists", 409));
        }

        string hashedPassword = _passwordHasher.HashPassword(password);

        var domainResult = Employee.Create(
            firstName: firstName,
            lastName: lastName,
            email: email,
            hashedPassword: hashedPassword,
            position: position.ToLower(),
            dateOfBirth: dateOfBirth,
            phoneNumber: phoneNumber,
            status: "active"
        );

        if (domainResult.IsFailed)
        {
            var error = domainResult.Errors[0];
            if (error is DomainError)
            {
                return Result.Fail(new ApplicationError(error.Message, 409));
            }
            return Result.Fail(new ApplicationError("Server error", 500));
        }

        Employee newEmployee = domainResult.Value;

        await _employeeRepository.AddEmployeeAsync(newEmployee);

        return Result.Ok();
    }

    public async Task<Result<EmployeeDto>> GetEmployee(string employeeId)
    {
        if (!Guid.TryParse(employeeId, out var employeeGuid))
        {
            return Result.Fail<EmployeeDto>(new ApplicationError("Invalid employee Id", 400));
        }

        Employee? employee = await _employeeRepository.GetEmployeeByIdAsync(employeeGuid);

        if (employee == null)
        {
            return Result.Fail<EmployeeDto>(new ApplicationError("Employee is not found", 404));
        }
        return MapToEmployeeDto(employee);
    }

    public async Task<Result<IEnumerable<EmployeeDto>>> GetAllEmployees()
    {
        IEnumerable<Employee> employees = await _employeeRepository.GetAllEmployeesAsync();

        List<EmployeeDto> employeeDtos = [];
        foreach (Employee employee in employees)
        {
            employeeDtos.Add(MapToEmployeeDto(employee));
        }
        return employeeDtos;
    }

    private static EmployeeDto MapToEmployeeDto(Employee employee)
    {   
        var today = DateTime.Today;
        var employeeAge = today.Year - employee.DateOfBirth.Year;
        
        if (employee.DateOfBirth.Date > today.AddYears(-employeeAge)) 
        {
            employeeAge--;
        }

        return new EmployeeDto(
            employee.Id,
            employee.FirstName,
            employee.LastName,
            employee.Email,
            employee.Position.ToStringValue(),
            employeeAge.ToString(),
            employee.PhoneNumber,
            employee.DateOfBirth,
            employee.JoinDate,
            employee.Status,
            employee.ImgUrl
        );
    }
}

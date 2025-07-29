using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Security;
using Application.DTOs.Employee;
using Domain.Entities;
using FluentResults;

namespace Application.Services.EmployeeService;

public class EmployeeService : BaseService, IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IPasswordHasher _passwordHasher;

    public EmployeeService(IEmployeeRepository employeeRepository, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork)
    : base(unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> AddEmployee(string firstName, string lastName, string email, string password, string position, string phoneNumber, string dateOfBirth)
    {
        if (await _employeeRepository.GetEmployeeByEmailAsync(email) is not null)
        {
            return Result.Fail(ApplicationError.Duplicated("Thi email already exists"));
        }

        string hashedPassword = _passwordHasher.HashPassword(password);

        var domainResult = Employee.Create(
            firstName: firstName,
            lastName: lastName,
            email: email,
            hashedPassword: hashedPassword,
            position: position.ToLower(),
            dateOfBirth: dateOfBirth,
            phoneNumber: phoneNumber
        );

        if (domainResult.IsFailed)
        {
            var error = domainResult.Errors[0];
            return Result.Fail(ApplicationError.Validation(error.Message));
        }

        Employee newEmployee = domainResult.Value;

        await _employeeRepository.AddEmployeeAsync(newEmployee);
        await _unitOfWork.SaveChangesAsync();

        return Result.Ok();
    }

    public async Task<Result<EmployeeDto>> GetEmployee(string employeeId)
    {
        if (!Guid.TryParse(employeeId, out var employeeGuid))
        {
            return Result.Fail<EmployeeDto>(ApplicationError.Validation("Invalid employee id"));
        }

        Employee? employee = await _employeeRepository.GetEmployeeByIdAsync(employeeGuid);

        if (employee == null)
        {
            return Result.Fail<EmployeeDto>(ApplicationError.NotFound("The employee is not found"));
        }

        await _unitOfWork.SaveChangesAsync();

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

        await _unitOfWork.SaveChangesAsync();

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
            employee.Position.ToString(),
            employeeAge.ToString(),
            employee.PhoneNumber,
            employee.DateOfBirth,
            employee.JoinDate,
            employee.Status.ToString(),
            employee.ImgUrl
        );
    }
}

using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Security;
using Application.DTOs.Employee;
using Domain.Employee;
using Domain.Employee.ValueObjects;
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
        var existingEmployeeResult = await _employeeRepository.GetEmployeeByEmailAsync(email);
        if (existingEmployeeResult.IsFailed) {
            return Result.Fail(ApplicationError.Internal);
        }

        if (existingEmployeeResult.Value is not null)
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

        var addResult = await _employeeRepository.AddEmployeeAsync(newEmployee);

        if (addResult.IsFailed)
        {
            return Result.Fail(ApplicationError.Internal);
        }

        await _unitOfWork.SaveChangesAsync();

        return Result.Ok();
    }

    public async Task<Result<EmployeeDto>> GetEmployee(string employeeId)
    {
        var employeeIdResult = EmployeeId.FromString(employeeId);
        if (employeeIdResult.IsFailed)
        {
            return Result.Fail<EmployeeDto>(ApplicationError.Validation("Invalid employee id"));
        }

        var employeeResult = await _employeeRepository.GetEmployeeByIdAsync(employeeIdResult.Value);
        if (employeeResult.IsFailed)
        {
            return Result.Fail<EmployeeDto>(ApplicationError.Internal);
        }

        if (employeeResult.Value is null)
        {
            return Result.Fail<EmployeeDto>(ApplicationError.NotFound("The employee is not found"));
        }

        await _unitOfWork.SaveChangesAsync();

        return MapToEmployeeDto(employeeResult.Value);
    }

    public async Task<Result<IEnumerable<EmployeeDto>>> GetAllEmployees()
    {
        var employeesResult = await _employeeRepository.GetAllEmployeesAsync();
        if (employeesResult.IsFailed)
        {
            return Result.Fail<IEnumerable<EmployeeDto>>(ApplicationError.Internal);
        }

        List<EmployeeDto> employeeDtos = [];
        foreach (Employee employee in employeesResult.Value)
        {
            employeeDtos.Add(MapToEmployeeDto(employee));
        }

        await _unitOfWork.SaveChangesAsync();

        return employeeDtos;
    }

    public async Task<Result<IEnumerable<EmployeeSearchDto>>> GetEmployeesByName(string name)
    {
        var employeesResult = await _employeeRepository.GetEmployeesByNameAsync(name);
        if (employeesResult.IsFailed)
        {
            return Result.Fail<IEnumerable<EmployeeSearchDto>>(ApplicationError.Internal);
        }

        List<EmployeeSearchDto> employeesSearchDto = [];
        foreach (Employee emp in employeesResult.Value)
        {
            employeesSearchDto.Add(MapToEmployeeSearchDto(emp));
        }

        await _unitOfWork.SaveChangesAsync();

        return employeesSearchDto;
    }

    private static EmployeeSearchDto MapToEmployeeSearchDto(Employee emp)
    {
        return new EmployeeSearchDto(emp.Id.ToString(),
                                     emp.FirstName,
                                     emp.LastName,
                                     emp.Email,
                                     emp.ImgUrl);
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
            employee.Id.ToString(),
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

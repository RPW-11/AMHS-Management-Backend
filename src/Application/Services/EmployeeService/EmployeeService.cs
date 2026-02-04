using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Security;
using Application.DTOs.Common;
using Application.DTOs.Employee;
using Domain.Employees;
using Domain.Employees.ValueObjects;
using Domain.Notifications;
using Domain.Notifications.ValueObjects;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Application.Services.EmployeeService;

public class EmployeeService : BaseService, IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(IEmployeeRepository employeeRepository,
                        INotificationRepository notificationRepository,
                        IPasswordHasher passwordHasher,
                        IUnitOfWork unitOfWork,
                        ILogger<EmployeeService> logger)
                        : base(unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _notificationRepository = notificationRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result> AddEmployee(
        string firstName,
        string lastName,
        string email,
        string password,
        string position,
        string phoneNumber,
        string dateOfBirth)
    {   
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Email"] = email
        });

        _logger.LogInformation("Add new employee request started for email {Email}", email);
        
        var existingEmployeeResult = await _employeeRepository.GetEmployeeByEmailAsync(email);
        if (existingEmployeeResult.IsFailed) {
            _logger.LogError("Failed to check existing employee by email: {ErrorMessage}", existingEmployeeResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        if (existingEmployeeResult.Value is not null)
        {
            _logger.LogInformation("Attempt to create duplicate employee - email already exists");
            return Result.Fail(ApplicationError.Duplicated("This email already exists"));
        }

        string hashedPassword;
        try
        {
            hashedPassword = _passwordHasher.HashPassword(password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hash password for new employee");
            return Result.Fail(ApplicationError.Internal);
        }

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
            var errorMessage = domainResult.Errors[0].Message;
            _logger.LogWarning("Domain validation failed when creating employee: {ErrorMessage}", errorMessage);
            return Result.Fail(ApplicationError.Validation(errorMessage));
        }

        Employee newEmployee = domainResult.Value;
        _logger.LogDebug("Employee domain entity created successfully for {Email}", email);

        var addResult = await _employeeRepository.AddEmployeeAsync(newEmployee);
        if (addResult.IsFailed)
        {
            _logger.LogError("Failed to add new employee to repository: {ErrorMessage}", addResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        // Notify the new user
        var notification = Notification.Create(
            recipientId: newEmployee.Id,
            actorId: null,
            actorName: "System",
            actorAvatarUrl: null,
            notificationTarget: NotificationTarget.Create(Guid.NewGuid(), "Welcome"),
            notificationType: NotificationType.FromString("info").Value, // optimistic
            Payload: $"Welcome to the company, {newEmployee.FirstName}!"
        );

        var notificationAddResult = await _notificationRepository.AddNotificationAsync(notification.Value);
        if (notificationAddResult.IsFailed)
        {
            _logger.LogError("Failed to add welcome notification for new employee: {ErrorMessage}", notificationAddResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Employee successfully created - ID: {EmployeeId}", newEmployee.Id);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database commit failed while creating new employee");
            return Result.Fail(ApplicationError.Internal);
        }
    }

    public async Task<Result<EmployeeDto>> GetEmployee(string employeeId)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EmployeeId"] = employeeId
        });

        _logger.LogInformation("Get employee request started");

        var employeeIdResult = EmployeeId.FromString(employeeId);
        if (employeeIdResult.IsFailed)
        {
            _logger.LogWarning("Invalid employee ID format: {ErrorMessage}",
                employeeIdResult.Errors[0].Message);
            return Result.Fail<EmployeeDto>(ApplicationError.Validation("Invalid employee id"));
        }

        var employeeResult = await _employeeRepository.GetEmployeeByIdAsync(employeeIdResult.Value);
        if (employeeResult.IsFailed)
        {
            _logger.LogError("Failed to retrieve employee from repository: {ErrorMessage}", employeeResult.Errors[0].Message);
            return Result.Fail<EmployeeDto>(ApplicationError.Internal);
        }

        if (employeeResult.Value is null)
        {
            _logger.LogInformation("Employee not found");
            return Result.Fail<EmployeeDto>(ApplicationError.NotFound("The employee is not found"));
        }

        var employeeDto = MapToEmployeeDto(employeeResult.Value);

        _logger.LogInformation("Employee retrieved successfully - Name: {EmployeeName}, Role/Position: {Position}",
            $"{employeeResult.Value.FirstName} {employeeResult.Value.LastName}",
            employeeResult.Value.Position);

        return employeeDto;
    }

    public async Task<Result<PagedResult<EmployeeDto>>> GetAllEmployees(int page, int pageSize, string? searchTerm = null)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Page"]       = page,
            ["PageSize"]   = pageSize,
            ["SearchTerm"] = searchTerm ?? "null"
        });

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 5, 100);

        _logger.LogDebug("Pagination adjusted → Page: {Page}, PageSize: {PageSize}", page, pageSize);  

        var employeeCountResult = await _employeeRepository.GetEmployeesCountAsync();
        if (employeeCountResult.IsFailed)
        {
            _logger.LogError("Failed to retrieve total employee count: {ErrorMessage}", employeeCountResult.Errors[0].Message);
            return Result.Fail<PagedResult<EmployeeDto>>(ApplicationError.Internal);
        }

        int totalCount = employeeCountResult.Value;
        _logger.LogDebug("Total employees in database: {TotalCount}", totalCount);

        var employeesResult = await _employeeRepository.GetAllEmployeesAsync(page, pageSize);
        if (employeesResult.IsFailed)
        {
            _logger.LogError("Failed to retrieve employees for page {Page} (size {PageSize}): {ErrorMessage}", 
                page, pageSize, employeesResult.Errors[0].Message);
            return Result.Fail<PagedResult<EmployeeDto>>(ApplicationError.Internal);
        }

        var employees = employeesResult.Value;
        _logger.LogDebug("Retrieved {FetchedCount} employees for current page", employees.Count());

        List<EmployeeDto> employeeDtos = [];
        foreach (Employee employee in employeesResult.Value)
        {
            employeeDtos.Add(MapToEmployeeDto(employee));
        }

        return new PagedResult<EmployeeDto>
        {
            Items = employeeDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = employeeCountResult.Value
        };
    }

    public async Task<Result<IEnumerable<EmployeeSearchDto>>> GetEmployeesByName(string name)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["SearchName"] = name
        });

        _logger.LogInformation("Employee name search started for: {SearchName}", name);

        var employeesResult = await _employeeRepository.GetEmployeesByNameAsync(name);
        if (employeesResult.IsFailed)
        {
            _logger.LogError("Failed to search employees by name in repository: {ErrorMessage}", employeesResult.Errors[0].Message);
            return Result.Fail<IEnumerable<EmployeeSearchDto>>(ApplicationError.Internal);
        }

        List<EmployeeSearchDto> employeesSearchDto = [];
        foreach (Employee emp in employeesResult.Value)
        {
            employeesSearchDto.Add(MapToEmployeeSearchDto(emp));
        }

        _logger.LogInformation("Successfully retrieved {Count} employees matching name: {SearchName}",
            employeesSearchDto.Count, name);

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

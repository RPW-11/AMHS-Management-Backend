using Application.DTOs.Employee;
using FluentResults;

namespace Application.Services.EmployeeService;

public interface IEmployeeService
{
    Task<Result> AddEmployee(string firstName, string lastName, string email, string password, string position, string phoneNumber, string dateOfBirth);
    Task<Result<EmployeeDto>> GetEmployee(string id);
    Task<Result<IEnumerable<EmployeeDto>>> GetAllEmployees();
    Task<Result<IEnumerable<EmployeeSearchDto>>> GetEmployeesByName(string name);
}

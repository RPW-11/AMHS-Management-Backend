using Domain.Employee;
using Domain.Employee.ValueObjects;
using FluentResults;

namespace Application.Common.Interfaces.Persistence;

public interface IEmployeeRepository
{
    Task<Result<Employee?>> GetEmployeeByEmailAsync(string email);
    Task<Result<Employee?>> GetEmployeeByIdAsync(EmployeeId id);
    Task<Result> AddEmployeeAsync(Employee employee);
    Task<Result<IEnumerable<Employee>>> GetAllEmployeesAsync();
}

using Domain.Employee;
using Domain.Employee.ValueObjects;
using FluentResults;

namespace Application.Common.Interfaces.Persistence;

public interface IEmployeeRepository
{
    Task<Result<Employee?>> GetEmployeeByEmailAsync(string email);
    Task<Result<Employee?>> GetEmployeeByIdAsync(EmployeeId id);
    Task<Result<IEnumerable<Employee>>> GetEmployeesByIdsAsync(IEnumerable<EmployeeId> employeeIds);
    Task<Result<IEnumerable<Employee>>> GetEmployeesByNameAsync(string name);
    Task<Result> AddEmployeeAsync(Employee employee);
    Task<Result<IEnumerable<Employee>>> GetAllEmployeesAsync(int page, int pageSize);
    Task<Result<int>> GetEmployeesCountAsync();
}

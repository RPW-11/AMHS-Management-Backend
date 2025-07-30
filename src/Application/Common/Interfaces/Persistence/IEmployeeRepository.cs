using Domain.Entities;
using FluentResults;

namespace Application.Common.Interfaces.Persistence;

public interface IEmployeeRepository
{
    Task<Result<Employee?>> GetEmployeeByEmailAsync(string email);
    Task<Result<Employee?>> GetEmployeeByIdAsync(Guid id);
    Task<Result> AddEmployeeAsync(Employee employee);
    Task<Result<IEnumerable<Employee>>> GetAllEmployeesAsync();
}

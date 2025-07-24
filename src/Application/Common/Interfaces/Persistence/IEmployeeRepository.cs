using Domain.Entities;

namespace Application.Common.Interfaces.Persistence;

public interface IEmployeeRepository
{
    Task<Employee?> GetEmployeeByEmailAsync(string email);
    Task<Employee?> GetEmployeeByIdAsync(Guid id);
    Task AddEmployeeAsync(Employee employee);
    Task<IEnumerable<Employee>> GetAllEmployeesAsync();
}

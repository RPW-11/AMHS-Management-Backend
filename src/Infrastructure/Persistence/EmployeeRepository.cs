using Application.Common.Interfaces.Persistence;
using Domain.Entities;

namespace Infrastructure.Persistence;

public class EmployeeRepository : IEmployeeRepository
{
    private static readonly List<Employee> _employees = [];

    public void AddEmployee(Employee employee)
    {
        _employees.Add(employee);
    }

    public Employee? GetEmployeeByEmail(string email)
    {
        return _employees.SingleOrDefault(e => e.Email.Value == email);
    }

    public Employee? GetEmployeeById(Guid id)
    {
        return _employees.SingleOrDefault(e => e.Id == id);
    }
}

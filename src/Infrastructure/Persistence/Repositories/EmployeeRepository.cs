using Application.Common.Interfaces.Persistence;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _dbContext;

    public EmployeeRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddEmployeeAsync(Employee employee)
    {
        await _dbContext.Employees.AddAsync(employee);
    }

    public async Task<Employee?> GetEmployeeByEmailAsync(string email)
    {
        Employee? employee = await _dbContext.Employees.FirstOrDefaultAsync(e => e.Email == email);
        return employee;
    }

    public async  Task<Employee?> GetEmployeeByIdAsync(Guid id)
    {
        Employee? employee = await _dbContext.Employees.FirstOrDefaultAsync(e => e.Id == id);
        return employee;
    }
}

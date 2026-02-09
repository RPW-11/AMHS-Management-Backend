using Application.Common.Interfaces.Persistence;
using Domain.Employees;
using Domain.Employees.ValueObjects;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _dbContext;

    public EmployeeRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> AddEmployeeAsync(Employee employee)
    {
        try
        {
            await _dbContext.Employees.AddAsync(employee);
            return Result.Ok();
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            return Result.Fail(new Error("Fail to insert the employee to the database").CausedBy(error));
        }
    }

    public async Task<Result<int>> GetEmployeesCountAsync()
    {
        try
        {
            int count = await _dbContext.Employees.CountAsync();

            return count;
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            return Result.Fail(new Error("Fail to get employees count from the database").CausedBy(error));
        }
    }

    public async Task<Result<IEnumerable<Employee>>> GetAllEmployeesAsync(int page, int pageSize)
    {
        try
        {
            IEnumerable<Employee> employees = await _dbContext.Employees
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Result.Ok(employees);
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            return Result.Fail(new Error("Fail to get employees from the database").CausedBy(error));
        }
    }

    public async Task<Result<Employee?>> GetEmployeeByEmailAsync(string email)
    {
        try
        {
            Employee? employee = await _dbContext.Employees.FirstOrDefaultAsync(e => e.Email == email);
            return employee;
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            return Result.Fail(new Error("Fail to get the employee by email from the database").CausedBy(error));
        }
    }

    public async Task<Result<Employee?>> GetEmployeeByIdAsync(EmployeeId id)
    {
        try
        {
            Employee? employee = await _dbContext.Employees.FirstOrDefaultAsync(e => e.Id == id);
            return employee;
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            return Result.Fail(new Error("Fail to get the employee by id from the database").CausedBy(error));
        }
    }

    public async Task<Result<IEnumerable<Employee>>> GetEmployeesByIdsAsync(IEnumerable<EmployeeId> employeeIds)
    {
        try
        {
            var employees = await _dbContext.Employees.Where(e => employeeIds.Contains(e.Id)).ToListAsync();
            return employees;
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            return Result.Fail(new Error("Fail to get the employees by ids from the database").CausedBy(error));
        }
    }

    public async Task<Result<IEnumerable<Employee>>> GetEmployeesByNameAsync(string name)
    {
        try
        {
            var pattern = $"{name.ToLower()}%";
            var employees = await _dbContext.Employees.Where(e => EF.Functions.Like(e.FirstName.ToLower(), pattern)
                                    ||
                                    EF.Functions.Like(e.LastName.ToLower(), pattern))
                                    .ToListAsync();
            return employees;
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            return Result.Fail(new Error("Fail to get the employees by name from the database").CausedBy(error));
        }
    }
}

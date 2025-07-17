using System;
using Domain.Entities;

namespace Domain.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee> GetEmployeeByIdAsync(string id);
    Task<Employee> GetEmployeeByEmail(string email);
}

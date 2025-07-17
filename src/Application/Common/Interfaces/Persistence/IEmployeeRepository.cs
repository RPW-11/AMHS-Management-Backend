using System;
using Domain.Entities;

namespace Application.Common.Interfaces.Persistence;

public interface IEmployeeRepository
{
    Employee? GetEmployeeByEmail(string email);
    Employee? GetEmployeeById(Guid id);
    void AddEmployee(Employee employee);
}

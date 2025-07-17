using System;
using Domain.Entities;

namespace Domain.Interfaces;

public interface IEmployeeFactory
{
    Task<Employee> CreateEmployeeAsync(string email, string firstName, string lastName, string position);
}

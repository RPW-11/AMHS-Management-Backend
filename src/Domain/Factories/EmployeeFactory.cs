using System;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Domain.Factories;

public class EmployeeFactory: IEmployeeFactory
{
    private readonly IEmployeeUniquenessChecker _uniquenessChecker;


    public EmployeeFactory(IEmployeeUniquenessChecker uniquenessChecker)
    {
        _uniquenessChecker = uniquenessChecker;
    }

    public async Task<Employee> CreateEmployeeAsync(string email, string firstName, string lastName, string position)
    {
        if (!await _uniquenessChecker.IsEmailUniqueAsync(email))
        {
            throw new DuplicateEmployeeEmailException(email);
        }

        var employeeEmail = new EmployeeEmail(email);
        var employeePosition = EmployeePositionExtension.ToEmployeePosition(position);
        return new Employee(firstName, lastName, employeeEmail, "DUNNO", employeePosition, new DateTime(2001,11,11), "ACTIVE");
    }
}

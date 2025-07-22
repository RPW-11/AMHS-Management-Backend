using System.Text.RegularExpressions;
using Domain.Enums.Employee;
using Domain.Errors.Employee;
using Domain.ValueObjects;
using FluentResults;

namespace Domain.Entities;

public class Employee
{
    public Guid Id { get; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string HashedPassword { get; private set; }
    public EmployeePositionEnum Position { get; private set; }
    public DateTime DateOfBirth { get; private set; }
    public DateTime JoinDate { get; private set; }
    public string Status { get; private set; }
    public string? ImgUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Employee(
        string firstName,
        string lastName,
        string email,
        string hashedPassword,
        EmployeePositionEnum position,
        DateTime dateOfBirth,
        string status
        )
    {
        Id = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        HashedPassword = hashedPassword;
        Position = position;
        DateOfBirth = dateOfBirth;
        JoinDate = DateTime.UtcNow;
        Status = status;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
        ImgUrl = null;
    }

    public static Result<Employee> Create(
        string firstName,
        string lastName,
        string email,
        string hashedPassword,
        string position,
        DateTime dateOfBirth,
        string status)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            return Result.Fail<Employee>(new EmptyEmployeeNameError());
        }

        if (!IsValidEmail(email))
        {
            return Result.Fail<Employee>(new InvalidEmployeeEmailFormatError(email));
        }

        var employeePosition = EmployeePositionExtension.ToEmployeePosition(position);

        if (employeePosition == EmployeePositionEnum.Invalid)
        {
            return Result.Fail<Employee>(new InvalidEmployeePositionError(position));
        }

        return new Employee(firstName, lastName, email, hashedPassword, employeePosition, dateOfBirth, status);
    }
    
    private static bool IsValidEmail(string email)
    {
        string pattern = @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$";

        return !string.IsNullOrWhiteSpace(email) && Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
    }
}

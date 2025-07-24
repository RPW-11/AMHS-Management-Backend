using System.Text.RegularExpressions;
using Domain.Enums.Employee;
using Domain.Errors.Employee;
using Domain.ValueObjects.Employee;
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
    public string PhoneNumber { get; private set; }
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
        string status,
        string phoneNumber
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
        PhoneNumber = phoneNumber;
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
        string dateOfBirth,
        string status,
        string phoneNumber)
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

        // validate the date input
        if (!DateTime.TryParse(dateOfBirth, out DateTime dateOfBirthParsed))
        {
            return Result.Fail<Employee>(new InvalidEmployeeDateOfBirthError());
        }

        if (!IsValidPhoneNumber(phoneNumber))
        {
            return Result.Fail<Employee>(new InvalidEmployeePhoneNumberError());
        }

        return new Employee(firstName, lastName, email, hashedPassword, employeePosition, dateOfBirthParsed, status, phoneNumber);
    }

    private static bool IsValidEmail(string email)
    {
        string pattern = @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$";

        return !string.IsNullOrWhiteSpace(email) && Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
    }

    private static bool IsValidPhoneNumber(string phoneNumber)
    {
        return !string.IsNullOrWhiteSpace(phoneNumber) && phoneNumber.Length >= 8;
    }
}

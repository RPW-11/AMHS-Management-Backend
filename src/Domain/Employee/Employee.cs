using System.Text.RegularExpressions;
using Domain.Common.Models;
using Domain.Employee.ValueObjects;
using Domain.Errors.Employee;
using FluentResults;

namespace Domain.Employee;

public sealed class Employee: AggregateRoot<EmployeeId>
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string HashedPassword { get; private set; }
    public EmployeePosition Position { get; private set; }
    public DateTime DateOfBirth { get; private set; }
    public DateTime JoinDate { get; private set; }
    public EmployeeStatus Status { get; private set; }
    public string PhoneNumber { get; private set; }
    public string? ImgUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Employee(
        EmployeeId id,
        string firstName,
        string lastName,
        string email,
        string hashedPassword,
        EmployeePosition position,
        DateTime dateOfBirth,
        EmployeeStatus status,
        string phoneNumber
        )
        : base(id)
    {
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

        var positionResult = EmployeePosition.FromString(position);

        if (positionResult.IsFailed)
        {
            return Result.Fail<Employee>(positionResult.Errors[0]);
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

        return new Employee(EmployeeId.CreateUnique(),
                            firstName,
                            lastName,
                            email,
                            hashedPassword,
                            positionResult.Value,
                            dateOfBirthParsed,
                            EmployeeStatus.Active,
                            phoneNumber);
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

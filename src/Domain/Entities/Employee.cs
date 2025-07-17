using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities;

public class Employee
{
    public Guid Id { get; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public EmployeeEmail Email { get; private set; }
    public string Password { get;  private set; }
    public EmployeePosition Position { get; private set; }
    public DateTime DateOfBirth { get; private set; }
    public DateTime JoinDate { get; private set; }
    public string Status { get; private set; }
    public string? ImgUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Employee(
        string firstName,
        string lastName,
        EmployeeEmail email,
        string password,
        EmployeePosition position,
        DateTime dateOfBirth,
        string status
        )
    {
        Id = Guid.NewGuid();
        FirstName = ValidateName(firstName);
        LastName = ValidateName(lastName, "last name");
        Email = email;
        Password = password;
        Position = position;
        DateOfBirth = dateOfBirth;
        JoinDate = new DateTime();
        Status = status;
        CreatedAt = new DateTime();
        UpdatedAt = CreatedAt;
    }

    private static string ValidateName(string name, string nameType = "first name")
    {
        if (String.IsNullOrWhiteSpace(name)) {
            throw new EmptyEmployeeNameException(nameType);
        }
        return name;
    }
}

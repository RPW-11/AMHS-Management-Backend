using System;

namespace Domain.Exceptions;

public sealed class InvalidEmployeeEmailFormatException : DomainException
{
    public string Email { get;  }
    public InvalidEmployeeEmailFormatException(string email) : base($"the email '{email}' is not a correct email")
    {
        Email = email;
    }
}

public sealed class DuplicateEmployeeEmailException : DomainException
{
    public string Email { get;  }
    public DuplicateEmployeeEmailException(string email) : base($"the email '{email}' already exists")
    {
        Email = email;
    }
}

public sealed class InvalidEmployeePositionException : DomainException
{
    public InvalidEmployeePositionException(string position) : base($"'{position}' is not a valid employee position")
    {

    }
}

public sealed class EmptyEmployeeNameException : DomainException
{
    public EmptyEmployeeNameException(string nameType = "first name") : base($"{nameType} cannot be empty")
    {
        
    }
}

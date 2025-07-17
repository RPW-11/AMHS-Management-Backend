using System;
using System.Text.RegularExpressions;
using Domain.Exceptions;

namespace Domain.ValueObjects;

public record EmployeeEmail
{
    public string Value { get; }

    public EmployeeEmail(string value)
    {
        if (!IsValid(value))
        {
            throw new InvalidEmployeeEmailFormatException(value);
        }

        Value = value;
    }

    private static bool IsValid(string email)
    {
        string pattern = @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$";

        return !String.IsNullOrWhiteSpace(email) && Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
    }

    public override string ToString() => Value;
}

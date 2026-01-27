using Domain.Common.Models;
using Domain.Errors.Employees;
using FluentResults;

namespace Domain.Employees.ValueObjects;

public class EmployeeStatus : ValueObject
{
    private enum StatusValue
    {
        Active,
        Inactive,
        Leave
    }

    private readonly StatusValue _value;

    private EmployeeStatus(StatusValue value)
    {
        _value = value;
    }

    public static EmployeeStatus Active => new(StatusValue.Active);
    public static EmployeeStatus Inactive => new(StatusValue.Inactive);
    public static EmployeeStatus Leave => new(StatusValue.Leave);

    public static Result<EmployeeStatus> FromString(string status)
    {
        if (string.IsNullOrEmpty(status))
        {
            return Result.Fail<EmployeeStatus>(new InvalidEmployeeStatusError(status));
        }

        return status.ToLower() switch
        {
            "active" => Active,
            "inactive" => Inactive,
            "leave" => Leave,
            _ => Result.Fail<EmployeeStatus>(new InvalidEmployeeStatusError(status))
        };
    }

    public override string ToString() => _value.ToString();
    
    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }
}

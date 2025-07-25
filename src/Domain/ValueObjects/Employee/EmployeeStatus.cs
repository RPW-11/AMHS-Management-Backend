using Domain.Errors.Employee;
using FluentResults;

namespace Domain.ValueObjects.Employee;

public class EmployeeStatus
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

    public override bool Equals(object? obj) => 
        obj is EmployeeStatus other && _value == other._value;

    public override int GetHashCode() => _value.GetHashCode();

    public static bool operator ==(EmployeeStatus left, EmployeeStatus right) => 
        left?.Equals(right) ?? right is null;

    public static bool operator !=(EmployeeStatus left, EmployeeStatus right) => 
        !(left == right);

}

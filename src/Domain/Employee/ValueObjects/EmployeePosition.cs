using Domain.Common.Models;
using Domain.Errors.Employee;
using FluentResults;

namespace Domain.Employee.ValueObjects;

public sealed class EmployeePosition : ValueObject
{
    private enum PositionValue
    {
        Staff,
        SeniorStaff,
        Supervisor,
        Manager,
    }

    private readonly PositionValue _value;

    private EmployeePosition(PositionValue value)
    {
        _value = value;
    }

    public static EmployeePosition Staff => new(PositionValue.Staff);
    public static EmployeePosition SeniorStaff => new(PositionValue.SeniorStaff);
    public static EmployeePosition Supervisor => new(PositionValue.Supervisor);
    public static EmployeePosition Manager => new(PositionValue.Manager);

    public static Result<EmployeePosition> FromString(string position)
    {
        if (string.IsNullOrEmpty(position))
        {
            return Result.Fail<EmployeePosition>(new InvalidEmployeePositionError(position));
        }

        return position.ToLower() switch
        {
            "staff" => Staff,
            "seniorstaff" => SeniorStaff,
            "supervisor" => Supervisor,
            "manager" => Manager,
            _ => Result.Fail<EmployeePosition>(new InvalidEmployeePositionError(position))
        };
    }

    public override string ToString() => _value.ToString();
    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }
}

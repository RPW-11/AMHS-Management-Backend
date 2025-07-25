using Domain.Errors.Employee;
using FluentResults;

namespace Domain.ValueObjects.Employee;

public class EmployeePosition
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

    public override bool Equals(object? obj) => 
        obj is EmployeePosition other && _value == other._value;

    public override int GetHashCode() => _value.GetHashCode();

    public static bool operator ==(EmployeePosition left, EmployeePosition right) => 
        left?.Equals(right) ?? right is null;

    public static bool operator !=(EmployeePosition left, EmployeePosition right) => 
        !(left == right);
}

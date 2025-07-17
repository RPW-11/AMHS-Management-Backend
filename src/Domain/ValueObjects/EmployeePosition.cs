using System;
using Domain.Exceptions;

namespace Domain.ValueObjects;

public enum EmployeePosition
{
    Staff,
    SeniorStaff,
    Supervisor,
    Manager

}

public static class EmployeePositionExtension
{
    private static readonly (EmployeePosition Position, string PositionStr)[] Mapping =
    {
            (EmployeePosition.Staff, "Staff"),
            (EmployeePosition.SeniorStaff, "Senior Staff"),
            (EmployeePosition.Supervisor, "Supervisor"),
            (EmployeePosition.Manager, "Manager")
        };

    public static string ToStringValue(this EmployeePosition position)
    {
        return Mapping.First(m => m.Position == position).PositionStr;
    }

    public static EmployeePosition ToEmployeePosition(this string position)
    {
        return position.ToLower() switch
        {
            "staff" => EmployeePosition.Staff,
            "senior staff" => EmployeePosition.SeniorStaff,
            "supervisor" => EmployeePosition.Supervisor,
            "manager" => EmployeePosition.Manager,
            _ => throw new InvalidEmployeePositionException(position)
        };
    }
}

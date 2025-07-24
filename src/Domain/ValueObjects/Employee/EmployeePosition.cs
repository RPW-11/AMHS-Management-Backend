using Domain.Enums.Employee;

namespace Domain.ValueObjects.Employee;

public static class EmployeePositionExtension
{
    private static readonly (EmployeePositionEnum Position, string PositionStr)[] Mapping =
    {
        (EmployeePositionEnum.Staff, "staff"),
        (EmployeePositionEnum.SeniorStaff, "senior staff"),
        (EmployeePositionEnum.Supervisor, "supervisor"),
        (EmployeePositionEnum.Manager, "manager")
    };

    public static string ToStringValue(this EmployeePositionEnum position)
    {
        return Mapping.First(m => m.Position == position).PositionStr;
    }

    public static EmployeePositionEnum ToEmployeePosition(this string position)
    {
        return position.ToLower() switch
        {
            "staff" => EmployeePositionEnum.Staff,
            "senior staff" => EmployeePositionEnum.SeniorStaff,
            "supervisor" => EmployeePositionEnum.Supervisor,
            "manager" => EmployeePositionEnum.Manager,
            _ => EmployeePositionEnum.Invalid
        };
    }
}

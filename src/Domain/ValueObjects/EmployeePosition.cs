using Domain.Enums.Employee;
namespace Domain.ValueObjects;


public static class EmployeePositionExtension
{
    private static readonly (EmployeePositionEnum Position, string PositionStr)[] Mapping =
    {
        (EmployeePositionEnum.Staff, "Staff"),
        (EmployeePositionEnum.SeniorStaff, "Senior Staff"),
        (EmployeePositionEnum.Supervisor, "Supervisor"),
        (EmployeePositionEnum.Manager, "Manager")
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

using Domain.Common.Models;
using FluentResults;

namespace Domain.Missions.ValueObjects;

public sealed class AssignedEmployeeId : ValueObject
{
    public string Value { get; }

    private AssignedEmployeeId(string missionId, string employeeId)
    {
        Value = missionId + employeeId;
    }
    public static AssignedEmployeeId CreateUnique(string missionId, string employeeId)
    {
        return new AssignedEmployeeId(missionId, employeeId);
    }
    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static Result<AssignedEmployeeId> FromString(string missionId, string employeeId)
    {
        return new AssignedEmployeeId(missionId, employeeId);
    }

    public override string ToString()
    {
        return Value;
    }
}

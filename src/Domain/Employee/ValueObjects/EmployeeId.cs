using Domain.Common.Models;
using Domain.Errors.Employee;
using FluentResults;

namespace Domain.Employee.ValueObjects;

public sealed class EmployeeId : ValueObject
{
    public Guid Value { get; }

    private EmployeeId(Guid id)
    {
        Value = id;
    }

    public static EmployeeId CreateUnique()
    {
        return new EmployeeId(Guid.NewGuid());
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static Result<EmployeeId> FromString(string value)
    {
        if (Guid.TryParse(value, out var id))
        {
            return new EmployeeId(id);
        }
        return Result.Fail<EmployeeId>(new InvalidEmployeeIdError(value));
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

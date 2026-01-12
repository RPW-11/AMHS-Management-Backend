using Domain.Employee;

namespace Application.Common.Interfaces.Authentication;

public interface IJwtTokenGenerator
{
    string GenerateToken(Employee employee);
}

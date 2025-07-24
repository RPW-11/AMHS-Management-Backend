using Application.Common.Interfaces.Security;

namespace Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;
    public string HashPassword(string rawPassword)
    {
        
        return BCrypt.Net.BCrypt.HashPassword(rawPassword, WorkFactor);
    }

    public bool VerifyPassword(string rawPassword, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(rawPassword, hashedPassword);
    }
}

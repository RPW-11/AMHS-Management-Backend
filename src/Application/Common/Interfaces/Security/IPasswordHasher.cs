namespace Application.Common.Interfaces.Security;

public interface IPasswordHasher
{
    string HashPassword(string rawPassword);
    bool VerifyPassword(string rawPassword, string hashedPassword);
}

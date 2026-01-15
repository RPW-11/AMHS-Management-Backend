
namespace Infrastructure.Authentication;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    public string Secret { get; init; } = "justsomerandomsecretkeyforjwtasfallback";
    public int ExpiryDays { get; init; }
    public string Issuer { get; init; } = "AMHSManagementAPI";
    public string Audience { get; init; } = "AMHSManagementClient";
}

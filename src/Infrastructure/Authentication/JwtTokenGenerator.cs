using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Common.Interfaces.Authentication;
using Application.Common.Interfaces.Services;
using Domain.Employee;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Authentication;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly JwtSettings _jwtSettings;

    public JwtTokenGenerator(IDateTimeProvider dateTimeProvider, IOptions<JwtSettings> jwtOptions)
    {
        _dateTimeProvider = dateTimeProvider;
        _jwtSettings = jwtOptions.Value;
    }
    public string GenerateToken(Employee employee)
    {
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
            SecurityAlgorithms.HmacSha256
        );

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, employee.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.GivenName, employee.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, employee.LastName),
            new Claim(JwtRegisteredClaimNames.Jti, employee.Id.ToString())
        };

        var securityToken = new JwtSecurityToken(
            claims: claims,
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            signingCredentials: signingCredentials,
            expires: _dateTimeProvider.UtcNow.AddDays(_jwtSettings.ExpiryDays)
        );

        return new JwtSecurityTokenHandler().WriteToken(securityToken);
    }
}

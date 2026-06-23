using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lloka.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lloka.Infrastructure.Services;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Secret        { get; set; } = string.Empty;
    public string Issuer        { get; set; } = string.Empty;
    public string Audience      { get; set; } = string.Empty;
    public int    ExpiresInDays { get; set; } = 7;
}

public class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    public string GenerateToken(Guid userId, string email, bool isOwner)
    {
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("isOwner",                     isOwner.ToString().ToLower()),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             options.Value.Issuer,
            audience:           options.Value.Audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddDays(options.Value.ExpiresInDays),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

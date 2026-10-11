using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using VeltrexAPI.Entities;

namespace VeltrexAPI.Services;

public class JwtOptions
{
    public string Issuer { get; set; } = "VeltrexAPI";
    public string Audience { get; set; } = "VeltrexApp";
    /// <summary>HMAC signing key; must be at least 32 characters.</summary>
    public string Key { get; set; } = "";
    public int ExpiryHours { get; set; } = 24;
}

public class TokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _opt = options.Value;

    public string CreateToken(User user)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _opt.Issuer,
            Audience = _opt.Audience,
            Expires = DateTime.UtcNow.AddHours(_opt.ExpiryHours),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("name", user.Name),
                new Claim("role", user.Role),
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key)), SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}

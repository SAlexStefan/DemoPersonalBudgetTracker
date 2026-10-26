using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PersonalBudgetTrackerApi.Models;

namespace PersonalBudgetTrackerApi.Services;

public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public string CreateToken(User user)
    {
        var claims = new List<Claim>
        {
           new(ClaimTypes.NameIdentifier, user.PublicId.ToString()),
           new(ClaimTypes.Email, user.Email)
        };

        var keyValue = _config["JWT:Secret"] ?? throw new InvalidOperationException("No JWT key is configured");
        var issuer = _config["JWT:Issuer"] ?? throw new InvalidOperationException("No JWT issuer is configured");
        var audience = _config["JWT:Audience"] ?? throw new InvalidOperationException("No JWT audience is configured");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyValue));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken
        (
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string RawToken, string Hash) GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var rawToken = Convert.ToBase64String(bytes);
        var hash = Convert.ToBase64String(SHA256.HashData(bytes));
        return (rawToken, hash);
    }

    public string HashRefreshToken(string rawToken)
    {
        var bytes = Convert.FromBase64String(rawToken);
        return Convert.ToBase64String(SHA256.HashData(bytes));
    }
}

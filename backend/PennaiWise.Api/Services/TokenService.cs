using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PennaiWise.Api.Models;

namespace PennaiWise.Api.Services;

public class TokenService(IConfiguration configuration)
{
    public string GenerateToken(User user)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var secret     = jwtSection["Secret"]   ?? throw new InvalidOperationException("JWT Secret is not configured.");
        var issuer     = jwtSection["Issuer"]   ?? throw new InvalidOperationException("JWT Issuer is not configured.");
        var audience   = jwtSection["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured.");

        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
        };

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

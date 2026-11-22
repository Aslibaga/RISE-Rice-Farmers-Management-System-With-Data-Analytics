using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using RISE.API.Dtos;
using RISE.API.Models;

namespace RISE.API.Services;

public interface ITokenService
{
    string GenerateToken(User user, IList<string> roles);
    string? GetUserIdFromContext(HttpContext context);
    string? GetUsernameFromContext(HttpContext context);
    string? GetRoleFromContext(HttpContext context);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user, IList<string> roles)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured")));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, user.DisplayName),
            new("userId", user.UserId.ToString()),
            new("role", roles.FirstOrDefault() ?? "DA Officer")
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string? GetUserIdFromContext(HttpContext context) =>
        context.User.FindFirst("userId")?.Value;

    public string? GetUsernameFromContext(HttpContext context) =>
        context.User.FindFirst(ClaimTypes.Name)?.Value;

    public string? GetRoleFromContext(HttpContext context) =>
        context.User.FindFirst("role")?.Value;
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ForgeMart.Api.Contracts;
using ForgeMart.Api.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ForgeMart.Api.Services;

namespace ForgeMart.Api.Authentication;

public interface ITokenService
{
    AuthResponse Create(User user);
}

public sealed class TokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenService
{
    private readonly JwtOptions jwt = options.Value;

    public AuthResponse Create(User user)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(jwt.ExpiryMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));
        var token = new JwtSecurityToken(
            jwt.Issuer,
            jwt.Audience,
            claims,
            now.UtcDateTime,
            expiresAt.UtcDateTime,
            new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            AuthService.ToResponse(user));
    }
}

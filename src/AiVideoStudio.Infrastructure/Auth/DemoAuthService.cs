using AiVideoStudio.Application.Auth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AiVideoStudio.Shared.Configuration;

namespace AiVideoStudio.Infrastructure.Auth;

public class DemoAuthService : IAuthService
{
    private readonly IAppConfiguration _configuration;

    public DemoAuthService(IAppConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<string?> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult<string?>(null);
        }

        var secret = _configuration.GetSetting("JwtSettings:Secret") ?? "ThisIsAVeryLongSecretKeyForJwtAuthenticationToWorkProperly";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration.GetSetting("JwtSettings:Issuer") ?? "AiVideoStudio",
            audience: _configuration.GetSetting("JwtSettings:Audience") ?? "AiVideoStudio",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return Task.FromResult<string?>(new JwtSecurityTokenHandler().WriteToken(token));
    }
}


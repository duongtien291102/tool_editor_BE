using AiVideoStudio.Application.Configuration;
using AiVideoStudio.Application.Interfaces.Auth;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Auth;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly IRefreshTokenRepository _repository;

    public RefreshTokenService(IOptions<JwtOptions> jwtOptions, IRefreshTokenRepository repository)
    {
        _jwtOptions = jwtOptions.Value;
        _repository = repository;
    }

    public async Task<RefreshTokenResult> GenerateRefreshTokenAsync(string userId, string familyId, string createdByIp, string? deviceId, string? userAgent, CancellationToken cancellationToken = default)
    {
        var randomBytes = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        var plainToken = Convert.ToBase64String(randomBytes);
        var tokenHash = HashToken(plainToken);

        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            JwtId = Guid.NewGuid().ToString(),
            FamilyId = string.IsNullOrEmpty(familyId) ? Guid.NewGuid().ToString() : familyId,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays),
            CreatedByIp = createdByIp,
            DeviceId = deviceId,
            UserAgent = userAgent
        };

        await _repository.AddAsync(entity, cancellationToken);

        return new RefreshTokenResult(plainToken, entity);
    }

    public string HashToken(string token)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_jwtOptions.RefreshTokenSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hash);
    }
}

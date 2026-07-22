using AiVideoStudio.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Application.Interfaces.Auth;

public record RefreshTokenResult(string PlainToken, RefreshToken Entity);

public interface IRefreshTokenService
{
    Task<RefreshTokenResult> GenerateRefreshTokenAsync(string userId, string familyId, string createdByIp, string? deviceId, string? userAgent, CancellationToken cancellationToken = default);
    string HashToken(string token);
}

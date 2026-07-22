using AiVideoStudio.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Domain.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<IEnumerable<RefreshToken>> GetFamilyAsync(string familyId, CancellationToken cancellationToken = default);
    Task RevokeFamilyAsync(string familyId, string reason, string revokedByIp, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(string userId, string reason, string revokedByIp, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task UpdateAsync(RefreshToken token, CancellationToken cancellationToken = default);
}

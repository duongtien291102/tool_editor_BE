using AiVideoStudio.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Domain.Interfaces;

public interface IPasswordHistoryRepository
{
    Task<System.Collections.Generic.IEnumerable<PasswordHistory>> GetRecentByUserIdAsync(string userId, int count = 5, CancellationToken cancellationToken = default);
    Task AddAsync(PasswordHistory entity, CancellationToken cancellationToken = default);
}

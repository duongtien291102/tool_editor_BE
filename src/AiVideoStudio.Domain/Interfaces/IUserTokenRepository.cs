using AiVideoStudio.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Domain.Interfaces;

public interface IUserTokenRepository
{
    Task<UserToken?> GetByHashAsync(string tokenHash, string purpose, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserToken entity, CancellationToken cancellationToken = default);
    Task AddAsync(UserToken entity, CancellationToken cancellationToken = default);
}

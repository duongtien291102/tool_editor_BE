using AiVideoStudio.Domain.Entities;

namespace AiVideoStudio.Domain.Interfaces;

public interface IAssetRepository
{
    Task<Asset?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task AddAsync(Asset entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Asset entity, CancellationToken cancellationToken = default);
}

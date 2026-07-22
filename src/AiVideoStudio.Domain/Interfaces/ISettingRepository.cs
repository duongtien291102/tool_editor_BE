using AiVideoStudio.Domain.Entities;

namespace AiVideoStudio.Domain.Interfaces;

public interface ISettingRepository
{
    Task<Setting?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task AddAsync(Setting entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Setting entity, CancellationToken cancellationToken = default);
}

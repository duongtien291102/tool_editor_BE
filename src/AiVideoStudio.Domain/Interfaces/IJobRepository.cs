using AiVideoStudio.Domain.Entities;

namespace AiVideoStudio.Domain.Interfaces;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task AddAsync(Job entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Job entity, CancellationToken cancellationToken = default);
}

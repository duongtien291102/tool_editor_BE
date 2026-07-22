using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Domain.Entities;

namespace AiVideoStudio.Domain.Interfaces;

public interface ITimelineRepository
{
    Task<Timeline?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Timeline?> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken = default);
    Task AddAsync(Timeline timeline, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Timeline timeline, int expectedVersion, CancellationToken cancellationToken = default);
    Task DeleteAsync(Timeline timeline, CancellationToken cancellationToken = default);
}

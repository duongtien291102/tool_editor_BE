using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Shared.Responses;

namespace AiVideoStudio.Domain.Interfaces;

public interface IRenderJobRepository
{
    Task<Entities.RenderJob?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task AddAsync(Entities.RenderJob job, CancellationToken cancellationToken = default);
    Task UpdateAsync(Entities.RenderJob job, CancellationToken cancellationToken = default);

    Task<PagedResult<Entities.RenderJob>> GetPagedAsync(
        string? ownerId,
        bool isAdmin,
        string? projectId,
        string? status,
        string? provider,
        string? priority,
        string? search,
        string? sortBy,
        bool sortDescending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Entities.RenderJob>> GetByProjectIdPagedAsync(
        string projectId,
        string? status,
        string? sortBy,
        bool sortDescending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

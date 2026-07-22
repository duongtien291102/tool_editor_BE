using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.Responses;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Domain.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task AddAsync(Project entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Project entity, CancellationToken cancellationToken = default);
    Task<PagedResult<Project>> GetPagedAsync(
        string? ownerId,
        bool isAdmin,
        int page,
        int pageSize,
        string? search,
        string? sortBy,
        bool sortDescending,
        ProjectStatus? status,
        CancellationToken cancellationToken = default);
}

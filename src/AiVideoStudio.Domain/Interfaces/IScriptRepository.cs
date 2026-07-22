using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Domain.Base;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Shared.Responses;

namespace AiVideoStudio.Domain.Interfaces;

public interface IScriptRepository
{
    Task<Script?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Script?> GetForUpdateAsync(string id, CancellationToken cancellationToken = default);
    Task<PagedResult<Script>> GetScriptsByProjectAsync(
        string projectId,
        string? searchTerm = null,
        bool includeDeleted = false,
        string? sortBy = null,
        bool descending = true,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
    
    Task AddAsync(Script script, CancellationToken cancellationToken = default);
    Task UpdateAsync(Script script, int expectedVersion, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(Script script, CancellationToken cancellationToken = default);
}

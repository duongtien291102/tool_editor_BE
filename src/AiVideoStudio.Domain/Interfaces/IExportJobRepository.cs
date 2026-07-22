using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Shared.Responses;

namespace AiVideoStudio.Domain.Interfaces;

public interface IExportJobRepository
{
    Task<ExportJob?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task AddAsync(ExportJob job, CancellationToken cancellationToken = default);
    Task UpdateAsync(ExportJob job, CancellationToken cancellationToken = default);
    Task<PagedResult<ExportJob>> GetByProjectIdPagedAsync(
        string projectId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

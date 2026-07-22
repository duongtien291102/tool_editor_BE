using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Shared.Responses;
namespace AiVideoStudio.Domain.Interfaces;

public interface IUploadSessionRepository
{
    Task<UploadSession?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task AddAsync(UploadSession session, CancellationToken cancellationToken = default);
    Task UpdateAsync(UploadSession session, CancellationToken cancellationToken = default);
    Task<PagedResult<UploadSession>> GetByProjectIdPagedAsync(string projectId, int page, int pageSize, CancellationToken cancellationToken = default);
}

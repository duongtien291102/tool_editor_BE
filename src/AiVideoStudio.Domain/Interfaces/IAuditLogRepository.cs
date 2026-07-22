using AiVideoStudio.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Domain.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog entity, CancellationToken cancellationToken = default);
    Task<AiVideoStudio.Shared.Responses.PagedResult<AuditLog>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}

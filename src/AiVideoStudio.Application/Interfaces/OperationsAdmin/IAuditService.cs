using AiVideoStudio.Domain.Entities.OperationsAdmin;

namespace AiVideoStudio.Application.Interfaces.OperationsAdmin;

public interface IAuditService
{
    Task LogAsync(
        string userId,
        string userName,
        string action,
        string resource,
        string resourceId,
        object? beforeState = null,
        object? afterState = null,
        string? correlationId = null,
        string? traceId = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlatformAuditLogEntry>> QueryAuditLogsAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);
}

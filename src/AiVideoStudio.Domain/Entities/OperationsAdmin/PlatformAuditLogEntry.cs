using AiVideoStudio.Domain.Base;

namespace AiVideoStudio.Domain.Entities.OperationsAdmin;

public sealed class PlatformAuditLogEntry : BaseEntity
{
    public string UserId { get; private set; } = string.Empty;
    public string UserName { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string Resource { get; private set; } = string.Empty;
    public string ResourceId { get; private set; } = string.Empty;
    public string? BeforeState { get; private set; }
    public string? AfterState { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public string TraceId { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;
    public DateTimeOffset Timestamp { get; private set; } = DateTimeOffset.UtcNow;

    private PlatformAuditLogEntry() { }

    public static PlatformAuditLogEntry Create(
        string userId,
        string userName,
        string action,
        string resource,
        string resourceId,
        string? beforeState,
        string? afterState,
        string correlationId,
        string traceId,
        string ipAddress,
        string userAgent)
    {
        return new PlatformAuditLogEntry
        {
            UserId = userId ?? "anonymous",
            UserName = userName ?? "anonymous",
            Action = action ?? "unknown",
            Resource = resource ?? "system",
            ResourceId = resourceId ?? string.Empty,
            BeforeState = beforeState,
            AfterState = afterState,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString("N"),
            TraceId = traceId ?? string.Empty,
            IpAddress = ipAddress ?? "127.0.0.1",
            UserAgent = userAgent ?? "internal",
            Timestamp = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = userId ?? "system"
        };
    }
}

using AiVideoStudio.Domain.Base;

namespace AiVideoStudio.Domain.Entities;

public class AuditLog : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty; // e.g., "Success", "Failed"
    public string IpAddress { get; set; } = string.Empty;
    public string Device { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public System.DateTimeOffset Timestamp { get; set; } = System.DateTimeOffset.UtcNow;
}

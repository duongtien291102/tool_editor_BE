using AiVideoStudio.Domain.Base;
using System;

namespace AiVideoStudio.Domain.Entities;

public class EmailOutbox : BaseEntity
{
    public string TemplateName { get; set; } = string.Empty;
    public string Variables { get; set; } = "{}"; // JSON string
    public string Status { get; set; } = "Pending"; // Pending, Sent, Failed
    public DateTimeOffset? NextRetryAt { get; set; }
    public int RetryCount { get; set; }
    public string LastError { get; set; } = string.Empty;
    public string ProcessingBy { get; set; } = string.Empty;
    public DateTimeOffset? LockedUntil { get; set; }
    public int Priority { get; set; }
}

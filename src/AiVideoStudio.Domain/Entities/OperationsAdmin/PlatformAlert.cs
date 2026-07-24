using AiVideoStudio.Domain.Base;

namespace AiVideoStudio.Domain.Entities.OperationsAdmin;

public sealed class PlatformAlert : BaseEntity
{
    public string RuleName { get; private set; } = string.Empty;
    public string MetricName { get; private set; } = string.Empty;
    public string Condition { get; private set; } = ">"; // >, <, ==, >=, <=
    public double Threshold { get; private set; }
    public string Severity { get; private set; } = "Warning"; // Info, Warning, Critical
    public bool IsActive { get; private set; } = true;
    public string NotificationChannel { get; private set; } = "Email"; // Email, Slack, Webhook
    public DateTimeOffset? LastTriggeredAt { get; private set; }

    private PlatformAlert() { }

    public static PlatformAlert Create(
        string ruleName,
        string metricName,
        string condition,
        double threshold,
        string severity,
        string notificationChannel,
        string createdBy)
    {
        return new PlatformAlert
        {
            RuleName = ruleName,
            MetricName = metricName,
            Condition = condition,
            Threshold = threshold,
            Severity = severity,
            NotificationChannel = notificationChannel,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = createdBy
        };
    }

    public void Trigger()
    {
        LastTriggeredAt = DateTimeOffset.UtcNow;
    }
}

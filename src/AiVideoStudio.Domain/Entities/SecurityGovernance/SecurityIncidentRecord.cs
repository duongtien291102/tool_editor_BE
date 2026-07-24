using AiVideoStudio.Domain.Base;

namespace AiVideoStudio.Domain.Entities.SecurityGovernance;

public sealed class SecurityIncidentRecord : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public string ThreatType { get; private set; } = "SuspiciousActivity"; // BruteForce, CredentialStuffing, ImpossibleTravel, TokenAbuse
    public string Severity { get; private set; } = "High"; // Low, Medium, High, Critical
    public string Status { get; private set; } = "Detected"; // Detected, Investigating, Mitigated, Resolved
    public string SourceIp { get; private set; } = string.Empty;
    public string? TargetUserId { get; private set; }
    public string? MitigationDetails { get; private set; }
    public List<SecuritySignal> Signals { get; private set; } = new();
    public List<IncidentTimelineEntry> Timeline { get; private set; } = new();

    private SecurityIncidentRecord() { }

    public static SecurityIncidentRecord Create(
        string title,
        string threatType,
        string severity,
        string sourceIp,
        string? targetUserId,
        string createdBy)
    {
        var record = new SecurityIncidentRecord
        {
            Title = title,
            ThreatType = threatType,
            Severity = severity,
            Status = "Detected",
            SourceIp = sourceIp,
            TargetUserId = targetUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = createdBy
        };

        record.Timeline.Add(new IncidentTimelineEntry("Threat Detected", $"Incident triggered by automated detection. IP: {sourceIp}", DateTimeOffset.UtcNow));
        return record;
    }

    public void AddSignal(string signalType, string payload, double confidence)
    {
        Signals.Add(new SecuritySignal(signalType, payload, confidence, DateTimeOffset.UtcNow));
    }

    public void Mitigate(string mitigationDetails, string updatedBy)
    {
        MitigationDetails = mitigationDetails;
        Status = "Mitigated";
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
        Timeline.Add(new IncidentTimelineEntry("Mitigated", mitigationDetails, DateTimeOffset.UtcNow));
    }

    public void Resolve(string resolutionSummary, string updatedBy)
    {
        Status = "Resolved";
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
        Timeline.Add(new IncidentTimelineEntry("Resolved", resolutionSummary, DateTimeOffset.UtcNow));
    }
}

public sealed record SecuritySignal(string SignalType, string Payload, double Confidence, DateTimeOffset Timestamp);
public sealed record IncidentTimelineEntry(string Phase, string Details, DateTimeOffset Timestamp);

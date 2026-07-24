using AiVideoStudio.Domain.Base;

namespace AiVideoStudio.Domain.Entities.OperationsAdmin;

public sealed class PlatformIncident : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Severity { get; private set; } = "P3"; // P1, P2, P3, P4
    public string Status { get; private set; } = "Open"; // Open, Investigating, Mitigated, Resolved
    public string? AssignedTo { get; private set; }
    public string? RootCause { get; private set; }
    public string? Resolution { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public List<IncidentTimelineItem> Timeline { get; private set; } = new();

    private PlatformIncident() { }

    public static PlatformIncident Create(string title, string description, string severity, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
        
        var incident = new PlatformIncident
        {
            Title = title.Trim(),
            Description = description ?? string.Empty,
            Severity = string.IsNullOrWhiteSpace(severity) ? "P3" : severity,
            Status = "Open",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = createdBy
        };

        incident.Timeline.Add(new IncidentTimelineItem("Incident Opened", $"Opened by {createdBy}", DateTimeOffset.UtcNow));
        return incident;
    }

    public void Assign(string assignee, string updatedBy)
    {
        AssignedTo = assignee;
        Status = "Investigating";
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
        Timeline.Add(new IncidentTimelineItem("Assigned", $"Assigned to {assignee}", DateTimeOffset.UtcNow));
    }

    public void Resolve(string rootCause, string resolution, string updatedBy)
    {
        RootCause = rootCause;
        Resolution = resolution;
        Status = "Resolved";
        ResolvedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
        Timeline.Add(new IncidentTimelineItem("Resolved", $"Resolved by {updatedBy}: {resolution}", DateTimeOffset.UtcNow));
    }
}

public sealed record IncidentTimelineItem(string Action, string Description, DateTimeOffset Timestamp);

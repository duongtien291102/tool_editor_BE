using AiVideoStudio.Domain.Base;

namespace AiVideoStudio.Domain.Entities.OperationsAdmin;

public sealed class MaintenanceWindow : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset ScheduledStart { get; private set; }
    public DateTimeOffset ScheduledEnd { get; private set; }
    public bool IsReadOnlyMode { get; private set; }
    public bool SystemRestartRequired { get; private set; }
    public string Status { get; private set; } = "Scheduled"; // Scheduled, InProgress, Completed, Cancelled

    private MaintenanceWindow() { }

    public static MaintenanceWindow Create(
        string title,
        string description,
        DateTimeOffset scheduledStart,
        DateTimeOffset scheduledEnd,
        bool isReadOnlyMode,
        bool systemRestartRequired,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
        if (scheduledEnd <= scheduledStart) throw new ArgumentException("End time must be after start time.", nameof(scheduledEnd));

        return new MaintenanceWindow
        {
            Title = title.Trim(),
            Description = description ?? string.Empty,
            ScheduledStart = scheduledStart,
            ScheduledEnd = scheduledEnd,
            IsReadOnlyMode = isReadOnlyMode,
            SystemRestartRequired = systemRestartRequired,
            Status = "Scheduled",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = createdBy
        };
    }

    public void StartMaintenance(string updatedBy)
    {
        Status = "InProgress";
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void CompleteMaintenance(string updatedBy)
    {
        Status = "Completed";
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}

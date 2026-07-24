using AiVideoStudio.Domain.Base;

namespace AiVideoStudio.Domain.Entities.OperationsAdmin;

public sealed class BackupSnapshot : BaseEntity
{
    public string SnapshotName { get; private set; } = string.Empty;
    public string BackupType { get; private set; } = "Full"; // Full, Incremental
    public string StorageLocation { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string Checksum { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Creating"; // Creating, Validated, Failed, Restored
    public DateTimeOffset? ValidatedAt { get; private set; }
    public string? ValidationResult { get; private set; }

    private BackupSnapshot() { }

    public static BackupSnapshot Create(
        string snapshotName,
        string backupType,
        string storageLocation,
        long sizeBytes,
        string checksum,
        string createdBy)
    {
        return new BackupSnapshot
        {
            SnapshotName = string.IsNullOrWhiteSpace(snapshotName) ? $"backup_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}" : snapshotName,
            BackupType = string.IsNullOrWhiteSpace(backupType) ? "Full" : backupType,
            StorageLocation = storageLocation ?? string.Empty,
            SizeBytes = sizeBytes,
            Checksum = checksum ?? string.Empty,
            Status = "Creating",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = createdBy
        };
    }

    public void Validate(bool success, string details, string updatedBy)
    {
        Status = success ? "Validated" : "Failed";
        ValidatedAt = DateTimeOffset.UtcNow;
        ValidationResult = details;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void MarkRestored(string updatedBy)
    {
        Status = "Restored";
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}

using AiVideoStudio.Domain.Base;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Events.Uploads;

namespace AiVideoStudio.Domain.Entities;

public sealed class UploadSession : BaseEntity
{
    private List<int> _completedChunks = new();
    public string AssetId { get; private set; } = string.Empty;
    public string ProjectId { get; private set; } = string.Empty;
    public string OwnerId { get; private set; } = string.Empty;
    public UploadStatus Status { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public long UploadedBytes { get; private set; }
    public int ChunkCount { get; private set; }
    public IReadOnlyCollection<int> CompletedChunks => _completedChunks.AsReadOnly();
    public string Checksum { get; private set; } = string.Empty;
    public string? StoragePath { get; private set; }
    public string? ManifestPath { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int Version { get; private set; }
    public int RetryCount { get; private set; }
    private UploadSession() { }

    public static UploadSession Create(string projectId, string ownerId, string fileName, string contentType,
        long fileSize, int chunkCount, string checksum)
    {
        if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(ownerId) || string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("Project, owner and file name are required.");
        if (fileSize <= 0) throw new ArgumentOutOfRangeException(nameof(fileSize));
        if (chunkCount <= 0) throw new ArgumentOutOfRangeException(nameof(chunkCount));
        if (string.IsNullOrWhiteSpace(checksum)) throw new ArgumentException("Checksum is required.", nameof(checksum));
        var session = new UploadSession
        {
            AssetId = Guid.NewGuid().ToString(),
            ProjectId = projectId,
            OwnerId = ownerId,
            FileName = Path.GetFileName(fileName),
            ContentType = contentType,
            FileSize = fileSize,
            ChunkCount = chunkCount,
            Checksum = checksum.ToLowerInvariant(),
            Status = UploadStatus.Pending,
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = ownerId
        };
        session.AddDomainEvent(new UploadStartedEvent(session.Id, ownerId));
        return session;
    }

    public bool HasChunk(int index) => _completedChunks.Contains(index);
    public void UploadChunk(int index, long bytes)
    {
        if (Status is not (UploadStatus.Pending or UploadStatus.Uploading)) throw new InvalidOperationException("Upload does not accept chunks.");
        if (index < 0 || index >= ChunkCount) throw new ArgumentOutOfRangeException(nameof(index));
        if (bytes <= 0 || UploadedBytes + bytes > FileSize) throw new ArgumentOutOfRangeException(nameof(bytes));
        if (HasChunk(index)) return;
        Status = UploadStatus.Uploading; _completedChunks.Add(index); _completedChunks.Sort(); UploadedBytes += bytes; Touch();
        AddDomainEvent(new ChunkUploadedEvent(Id, index, bytes));
    }
    public void Merge()
    {
        if (Status != UploadStatus.Uploading || _completedChunks.Count != ChunkCount || UploadedBytes != FileSize) throw new InvalidOperationException("Upload is incomplete.");
        Status = UploadStatus.Merging; Touch();
    }
    public void Complete(string storagePath, string manifestPath)
    {
        if (Status != UploadStatus.Merging) throw new InvalidOperationException("Upload is not merging.");
        if (string.IsNullOrWhiteSpace(storagePath) || string.IsNullOrWhiteSpace(manifestPath)) throw new ArgumentException("Storage and manifest paths are required.");
        StoragePath = storagePath; ManifestPath = manifestPath; Status = UploadStatus.Completed; CompletedAt = DateTimeOffset.UtcNow; ErrorMessage = null; Touch();
        AddDomainEvent(new UploadCompletedEvent(Id, AssetId));
    }
    public void Fail(string error) { if (Status is UploadStatus.Completed or UploadStatus.Cancelled) throw new InvalidOperationException(); Status = UploadStatus.Failed; ErrorMessage = error; Touch(); AddDomainEvent(new UploadFailedEvent(Id, error)); }
    public void Cancel() { if (Status is UploadStatus.Completed or UploadStatus.Cancelled) throw new InvalidOperationException(); Status = UploadStatus.Cancelled; Touch(); AddDomainEvent(new UploadCancelledEvent(Id, OwnerId)); }
    public void Retry() { if (Status != UploadStatus.Failed) throw new InvalidOperationException(); Status = _completedChunks.Count == 0 ? UploadStatus.Pending : UploadStatus.Uploading; ErrorMessage = null; RetryCount++; Touch(); }
    private void Touch() { Version++; UpdatedAt = DateTimeOffset.UtcNow; UpdatedBy = OwnerId; }
}

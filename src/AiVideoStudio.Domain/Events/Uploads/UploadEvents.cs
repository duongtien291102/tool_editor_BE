using AiVideoStudio.Domain.Base;
namespace AiVideoStudio.Domain.Events.Uploads;

public record UploadStartedEvent(string UploadId, string OwnerId) : IDomainEvent { public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow; }
public record ChunkUploadedEvent(string UploadId, int ChunkIndex, long Bytes) : IDomainEvent { public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow; }
public record UploadCompletedEvent(string UploadId, string AssetId) : IDomainEvent { public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow; }
public record UploadFailedEvent(string UploadId, string Error) : IDomainEvent { public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow; }
public record UploadCancelledEvent(string UploadId, string OwnerId) : IDomainEvent { public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow; }

using AiVideoStudio.Domain.Base;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Events.Exports;

namespace AiVideoStudio.Domain.Entities;

public sealed class ExportJob : BaseEntity
{
    public string RenderJobId { get; private set; } = string.Empty;
    public string ProjectId { get; private set; } = string.Empty;
    public string TimelineId { get; private set; } = string.Empty;
    public string OwnerId { get; private set; } = string.Empty;
    public ExportStatus Status { get; private set; }
    public int Progress { get; private set; }
    public string? OutputPath { get; private set; }
    public TimeSpan Duration { get; private set; }
    public string Resolution { get; private set; } = string.Empty;
    public double FrameRate { get; private set; }
    public VideoCodec VideoCodec { get; private set; }
    public AudioCodec AudioCodec { get; private set; }
    public ContainerFormat Container { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int Version { get; private set; }
    public int RetryCount { get; private set; }
    public int MaxRetryCount { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool IsDeleted => DeletedAt.HasValue;

    private ExportJob() { }

    public static ExportJob Create(
        string renderJobId,
        string projectId,
        string timelineId,
        string ownerId,
        TimeSpan duration,
        string resolution,
        double frameRate,
        VideoCodec videoCodec,
        AudioCodec audioCodec,
        ContainerFormat container,
        int maxRetryCount = 3)
    {
        if (string.IsNullOrWhiteSpace(renderJobId)) throw new ArgumentException("RenderJobId is required.", nameof(renderJobId));
        if (string.IsNullOrWhiteSpace(projectId)) throw new ArgumentException("ProjectId is required.", nameof(projectId));
        if (string.IsNullOrWhiteSpace(timelineId)) throw new ArgumentException("TimelineId is required.", nameof(timelineId));
        if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("OwnerId is required.", nameof(ownerId));
        if (duration < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
        if (string.IsNullOrWhiteSpace(resolution)) throw new ArgumentException("Resolution is required.", nameof(resolution));
        if (frameRate <= 0) throw new ArgumentOutOfRangeException(nameof(frameRate));
        if (maxRetryCount is < 0 or > 10) throw new ArgumentOutOfRangeException(nameof(maxRetryCount));

        return new ExportJob
        {
            RenderJobId = renderJobId,
            ProjectId = projectId,
            TimelineId = timelineId,
            OwnerId = ownerId,
            Status = ExportStatus.Pending,
            Progress = 0,
            Duration = duration,
            Resolution = resolution.Trim(),
            FrameRate = frameRate,
            VideoCodec = videoCodec,
            AudioCodec = audioCodec,
            Container = container,
            Version = 1,
            MaxRetryCount = maxRetryCount,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = ownerId
        };
    }

    public void Start()
    {
        RequireStatus(ExportStatus.Pending);
        Status = ExportStatus.Preparing;
        Progress = 0;
        StartedAt = DateTimeOffset.UtcNow;
        Touch(OwnerId);
        AddDomainEvent(new ExportStartedEvent(Id, OwnerId));
    }

    public void MarkRendering()
    {
        RequireStatus(ExportStatus.Preparing);
        Status = ExportStatus.Rendering;
        Touch(OwnerId);
    }

    public void MarkMuxing()
    {
        RequireStatus(ExportStatus.Rendering);
        Status = ExportStatus.Muxing;
        Touch(OwnerId);
    }

    public void UpdateProgress(int progress)
    {
        if (Status is not (ExportStatus.Preparing or ExportStatus.Rendering or ExportStatus.Muxing))
            throw new InvalidOperationException($"Cannot update progress while export is {Status}.");
        if (progress is < 0 or > 99) throw new ArgumentOutOfRangeException(nameof(progress));
        if (progress < Progress) throw new InvalidOperationException("Export progress cannot decrease.");

        Progress = progress;
        Touch(OwnerId);
        AddDomainEvent(new ExportProgressChangedEvent(Id, progress));
    }

    public void Complete(string outputPath)
    {
        RequireStatus(ExportStatus.Muxing);
        if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("OutputPath is required.", nameof(outputPath));
        Status = ExportStatus.Completed;
        Progress = 100;
        OutputPath = outputPath;
        CompletedAt = DateTimeOffset.UtcNow;
        ErrorCode = null;
        ErrorMessage = null;
        Touch(OwnerId);
        AddDomainEvent(new ExportCompletedEvent(Id, outputPath));
    }

    public void Fail(string errorMessage, string? errorCode = null)
    {
        if (Status is ExportStatus.Completed or ExportStatus.Cancelled or ExportStatus.Failed)
            throw new InvalidOperationException($"Cannot fail export while it is {Status}.");
        if (string.IsNullOrWhiteSpace(errorMessage)) throw new ArgumentException("Error message is required.", nameof(errorMessage));
        Status = ExportStatus.Failed;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
        Touch(OwnerId);
        AddDomainEvent(new ExportFailedEvent(Id, errorMessage));
    }

    public void Cancel(string cancelledBy)
    {
        if (Status is ExportStatus.Completed or ExportStatus.Failed or ExportStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel export while it is {Status}.");
        Status = ExportStatus.Cancelled;
        Touch(cancelledBy);
        AddDomainEvent(new ExportCancelledEvent(Id, cancelledBy));
    }

    public void Retry(string retriedBy)
    {
        RequireStatus(ExportStatus.Failed);
        if (RetryCount >= MaxRetryCount) throw new InvalidOperationException("Maximum retry count reached.");
        RetryCount++;
        Status = ExportStatus.Pending;
        Progress = 0;
        OutputPath = null;
        StartedAt = null;
        CompletedAt = null;
        ErrorCode = null;
        ErrorMessage = null;
        Touch(retriedBy);
    }

    private void Touch(string updatedBy)
    {
        Version++;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    private void RequireStatus(ExportStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException($"Invalid export transition: {Status} -> expected {expected}.");
    }
}

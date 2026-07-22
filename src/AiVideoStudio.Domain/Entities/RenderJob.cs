using System;
using System.Text.Json;
using AiVideoStudio.Domain.Base;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Events.RenderJobs;

namespace AiVideoStudio.Domain.Entities;

/// <summary>
/// RenderJob Aggregate Root.
/// Manages the full lifecycle of a render/AI generation task.
/// All state transitions are enforced via business methods.
/// </summary>
public class RenderJob : BaseEntity
{
    public string ProjectId { get; private set; } = string.Empty;
    public string? TimelineId { get; private set; }
    public string? ScriptId { get; private set; }
    public string OwnerId { get; private set; } = string.Empty;
    public RenderJobType JobType { get; private set; }
    public RenderProvider Provider { get; private set; }
    public RenderJobStatus Status { get; private set; }
    public RenderPriority Priority { get; private set; }

    /// <summary>Progress value: 0–100</summary>
    public int Progress { get; private set; }

    public int RetryCount { get; private set; }
    public int MaxRetryCount { get; private set; }

    /// <summary>Input payload as JSON document. Strongly typed, never object/dynamic.</summary>
    public JsonDocument? InputPayload { get; private set; }

    /// <summary>Output payload as JSON document. Strongly typed, never object/dynamic.</summary>
    public JsonDocument? OutputPayload { get; private set; }

    public string? ErrorMessage { get; private set; }
    public string? ErrorCode { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public bool IsDeleted => DeletedAt.HasValue;

    // Parameterless constructor for MongoDB deserialization
    protected RenderJob() { }

    /// <summary>
    /// Factory — creates a new RenderJob in Pending status.
    /// </summary>
    public static RenderJob Create(
        string projectId,
        string ownerId,
        RenderJobType jobType,
        RenderProvider provider,
        RenderPriority priority = RenderPriority.Normal,
        int maxRetryCount = 3,
        string? timelineId = null,
        string? scriptId = null,
        JsonDocument? inputPayload = null)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            throw new ArgumentException("ProjectId cannot be empty.", nameof(projectId));
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("OwnerId cannot be empty.", nameof(ownerId));
        if (maxRetryCount < 0)
            throw new ArgumentOutOfRangeException(nameof(maxRetryCount), "MaxRetryCount must be >= 0.");

        var job = new RenderJob
        {
            ProjectId = projectId,
            OwnerId = ownerId,
            JobType = jobType,
            Provider = provider,
            Priority = priority,
            Status = RenderJobStatus.Pending,
            Progress = 0,
            RetryCount = 0,
            MaxRetryCount = maxRetryCount,
            TimelineId = timelineId,
            ScriptId = scriptId,
            InputPayload = inputPayload,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = ownerId
        };

        return job;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // State Machine Transitions
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Pending → Queued</summary>
    public void Queue()
    {
        ThrowIfInvalidTransition(
            RenderJobStatus.Queued,
            RenderJobStatus.Pending);

        Status = RenderJobStatus.Queued;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new RenderJobQueuedEvent(Id, OwnerId));
    }

    /// <summary>Queued → Processing</summary>
    public void Start()
    {
        ThrowIfInvalidTransition(
            RenderJobStatus.Processing,
            RenderJobStatus.Queued);

        Status = RenderJobStatus.Processing;
        StartedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new RenderJobStartedEvent(Id, OwnerId));
    }

    /// <summary>
    /// Updates progress (0–100). Only valid while Processing.
    /// </summary>
    public void UpdateProgress(int progress)
    {
        if (Status != RenderJobStatus.Processing)
            throw new InvalidOperationException(
                $"Cannot update progress. Current status: {Status}.");

        if (progress < 0 || progress > 100)
            throw new ArgumentOutOfRangeException(nameof(progress), "Progress must be between 0 and 100.");

        Progress = progress;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new RenderJobProgressChangedEvent(Id, Progress));
    }

    /// <summary>Processing → Completed. Sets Progress = 100.</summary>
    public void Complete(JsonDocument? outputPayload = null)
    {
        ThrowIfInvalidTransition(
            RenderJobStatus.Completed,
            RenderJobStatus.Processing);

        Status = RenderJobStatus.Completed;
        Progress = 100;
        OutputPayload = outputPayload;
        CompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new RenderJobCompletedEvent(Id, OwnerId));
    }

    /// <summary>Processing → Failed. Retains current progress.</summary>
    public void Fail(string errorMessage, string? errorCode = null)
    {
        ThrowIfInvalidTransition(
            RenderJobStatus.Failed,
            RenderJobStatus.Processing);

        Status = RenderJobStatus.Failed;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new RenderJobFailedEvent(Id, errorMessage));
    }

    /// <summary>
    /// Failed → Queued (Retry).
    /// Enforces MaxRetryCount and exponential backoff is handled by Worker.
    /// </summary>
    public void Retry(string updatedBy)
    {
        if (Status == RenderJobStatus.Completed)
            throw new InvalidOperationException("Completed jobs cannot be retried.");
        if (Status == RenderJobStatus.Cancelled)
            throw new InvalidOperationException("Cancelled jobs cannot be retried.");
        if (Status != RenderJobStatus.Failed)
            throw new InvalidOperationException(
                $"Cannot retry. Current status: {Status}. Only Failed jobs can be retried.");
        if (RetryCount >= MaxRetryCount)
            throw new InvalidOperationException(
                $"Max retry count ({MaxRetryCount}) reached.");

        RetryCount++;
        Status = RenderJobStatus.Queued;
        ErrorMessage = null;
        ErrorCode = null;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
        AddDomainEvent(new RenderJobQueuedEvent(Id, OwnerId));
    }

    /// <summary>
    /// Pending / Queued / Processing → Cancelled.
    /// Cannot cancel Completed or already Cancelled jobs.
    /// </summary>
    public void Cancel(string cancelledBy)
    {
        if (Status == RenderJobStatus.Completed)
            throw new InvalidOperationException("Completed jobs cannot be cancelled.");
        if (Status == RenderJobStatus.Cancelled)
            throw new InvalidOperationException("Job is already cancelled.");
        if (Status == RenderJobStatus.Failed)
            throw new InvalidOperationException("Failed jobs cannot be cancelled. Use Retry instead.");

        Status = RenderJobStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = cancelledBy;
        AddDomainEvent(new RenderJobCancelledEvent(Id, cancelledBy));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    public bool CanRetry => Status == RenderJobStatus.Failed && RetryCount < MaxRetryCount;
    public bool CanCancel => Status is RenderJobStatus.Pending or RenderJobStatus.Queued or RenderJobStatus.Processing;

    private void ThrowIfInvalidTransition(RenderJobStatus target, params RenderJobStatus[] allowedSources)
    {
        foreach (var allowed in allowedSources)
        {
            if (Status == allowed) return;
        }

        throw new InvalidOperationException(
            $"Invalid status transition: {Status} → {target}.");
    }
}

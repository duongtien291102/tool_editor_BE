using System;
using AiVideoStudio.Domain.Base;

namespace AiVideoStudio.Domain.Events.RenderJobs;

public record RenderJobQueuedEvent(string JobId, string OwnerId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public record RenderJobStartedEvent(string JobId, string OwnerId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public record RenderJobProgressChangedEvent(string JobId, int Progress) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public record RenderJobCompletedEvent(string JobId, string OwnerId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public record RenderJobFailedEvent(string JobId, string ErrorMessage) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public record RenderJobCancelledEvent(string JobId, string CancelledBy) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

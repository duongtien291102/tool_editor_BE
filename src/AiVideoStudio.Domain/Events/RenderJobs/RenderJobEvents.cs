using AiVideoStudio.Domain.Base;

namespace AiVideoStudio.Domain.Events.RenderJobs;

public record RenderJobQueuedEvent(string JobId, string OwnerId) : IDomainEvent;
public record RenderJobStartedEvent(string JobId, string OwnerId) : IDomainEvent;
public record RenderJobProgressChangedEvent(string JobId, int Progress) : IDomainEvent;
public record RenderJobCompletedEvent(string JobId, string OwnerId) : IDomainEvent;
public record RenderJobFailedEvent(string JobId, string ErrorMessage) : IDomainEvent;
public record RenderJobCancelledEvent(string JobId, string CancelledBy) : IDomainEvent;

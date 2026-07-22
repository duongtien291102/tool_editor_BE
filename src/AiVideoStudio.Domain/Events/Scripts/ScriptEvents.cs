using System;
using AiVideoStudio.Domain.Base;

namespace AiVideoStudio.Domain.Events.Scripts;

public record ScriptCreatedEvent(string ScriptId, string ProjectId, string OwnerId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public record ScriptUpdatedEvent(string ScriptId, string UpdatedBy) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public record ScriptDeletedEvent(string ScriptId, string DeletedBy) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public record SceneAddedEvent(string ScriptId, string SceneId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public record SceneRemovedEvent(string ScriptId, string SceneId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public record SceneUpdatedEvent(string ScriptId, string SceneId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public record SceneElementUpdatedEvent(string ScriptId, string SceneId, string ElementId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

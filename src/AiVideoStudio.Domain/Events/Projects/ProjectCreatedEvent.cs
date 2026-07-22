using AiVideoStudio.Domain.Base;
using System;

namespace AiVideoStudio.Domain.Events.Projects;

public class ProjectCreatedEvent : IDomainEvent
{
    public string ProjectId { get; }
    public string OwnerId { get; }
    public DateTimeOffset OccurredOn { get; }

    public ProjectCreatedEvent(string projectId, string ownerId)
    {
        ProjectId = projectId;
        OwnerId = ownerId;
        OccurredOn = DateTimeOffset.UtcNow;
    }
}

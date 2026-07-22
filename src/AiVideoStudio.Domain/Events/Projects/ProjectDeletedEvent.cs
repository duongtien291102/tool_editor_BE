using AiVideoStudio.Domain.Base;
using System;

namespace AiVideoStudio.Domain.Events.Projects;

public class ProjectDeletedEvent : IDomainEvent
{
    public string ProjectId { get; }
    public string DeletedBy { get; }
    public DateTimeOffset OccurredOn { get; }

    public ProjectDeletedEvent(string projectId, string deletedBy)
    {
        ProjectId = projectId;
        DeletedBy = deletedBy;
        OccurredOn = DateTimeOffset.UtcNow;
    }
}

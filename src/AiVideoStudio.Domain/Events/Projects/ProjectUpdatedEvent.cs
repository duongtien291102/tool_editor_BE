using AiVideoStudio.Domain.Base;
using System;

namespace AiVideoStudio.Domain.Events.Projects;

public class ProjectUpdatedEvent : IDomainEvent
{
    public string ProjectId { get; }
    public string UpdatedBy { get; }
    public DateTimeOffset OccurredOn { get; }

    public ProjectUpdatedEvent(string projectId, string updatedBy)
    {
        ProjectId = projectId;
        UpdatedBy = updatedBy;
        OccurredOn = DateTimeOffset.UtcNow;
    }
}

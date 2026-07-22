using AiVideoStudio.Domain.Base;
using System;

namespace AiVideoStudio.Domain.Events.Media;

public class MediaUploadedEvent : IDomainEvent
{
    public string MediaId { get; }
    public string ProjectId { get; }
    public string OwnerId { get; }
    public DateTimeOffset OccurredOn { get; }

    public MediaUploadedEvent(string mediaId, string projectId, string ownerId)
    {
        MediaId = mediaId;
        ProjectId = projectId;
        OwnerId = ownerId;
        OccurredOn = DateTimeOffset.UtcNow;
    }
}

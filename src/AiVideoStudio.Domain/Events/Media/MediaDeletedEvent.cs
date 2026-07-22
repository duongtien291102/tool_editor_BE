using AiVideoStudio.Domain.Base;
using System;

namespace AiVideoStudio.Domain.Events.Media;

public class MediaDeletedEvent : IDomainEvent
{
    public string MediaId { get; }
    public string DeletedBy { get; }
    public DateTimeOffset OccurredOn { get; }

    public MediaDeletedEvent(string mediaId, string deletedBy)
    {
        MediaId = mediaId;
        DeletedBy = deletedBy;
        OccurredOn = DateTimeOffset.UtcNow;
    }
}

using AiVideoStudio.Domain.Base;
using System;

namespace AiVideoStudio.Domain.Events.Media;

public class MediaProcessingStartedEvent : IDomainEvent
{
    public string MediaId { get; }
    public DateTimeOffset OccurredOn { get; }

    public MediaProcessingStartedEvent(string mediaId)
    {
        MediaId = mediaId;
        OccurredOn = DateTimeOffset.UtcNow;
    }
}

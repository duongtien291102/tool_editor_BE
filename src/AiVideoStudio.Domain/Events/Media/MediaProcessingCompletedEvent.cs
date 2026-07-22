using AiVideoStudio.Domain.Base;
using System;

namespace AiVideoStudio.Domain.Events.Media;

public class MediaProcessingCompletedEvent : IDomainEvent
{
    public string MediaId { get; }
    public bool Success { get; }
    public DateTimeOffset OccurredOn { get; }

    public MediaProcessingCompletedEvent(string mediaId, bool success)
    {
        MediaId = mediaId;
        Success = success;
        OccurredOn = DateTimeOffset.UtcNow;
    }
}

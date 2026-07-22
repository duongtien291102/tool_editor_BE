using AiVideoStudio.Domain.Base;
using System;

namespace AiVideoStudio.Domain.Events.Auth;

public class RefreshTokenRotatedEvent : IDomainEvent
{
    public string UserId { get; }
    public string OldFamilyId { get; }
    public DateTimeOffset OccurredOn { get; }

    public RefreshTokenRotatedEvent(string userId, string oldFamilyId)
    {
        UserId = userId;
        OldFamilyId = oldFamilyId;
        OccurredOn = DateTimeOffset.UtcNow;
    }
}

using AiVideoStudio.Domain.Base;
using System;

namespace AiVideoStudio.Domain.Events.Auth;

public class RefreshTokenCompromisedEvent : IDomainEvent
{
    public string UserId { get; }
    public string FamilyId { get; }
    public string SuspectedIp { get; }
    public DateTimeOffset OccurredOn { get; }

    public RefreshTokenCompromisedEvent(string userId, string familyId, string suspectedIp)
    {
        UserId = userId;
        FamilyId = familyId;
        SuspectedIp = suspectedIp;
        OccurredOn = DateTimeOffset.UtcNow;
    }
}

using AiVideoStudio.Domain.Base;
using System;

namespace AiVideoStudio.Domain.Events.Auth;

public class UserLoggedInEvent : IDomainEvent
{
    public string UserId { get; }
    public string IpAddress { get; }
    public DateTimeOffset OccurredOn { get; }

    public UserLoggedInEvent(string userId, string ipAddress)
    {
        UserId = userId;
        IpAddress = ipAddress;
        OccurredOn = DateTimeOffset.UtcNow;
    }
}

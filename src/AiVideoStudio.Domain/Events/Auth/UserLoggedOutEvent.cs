using AiVideoStudio.Domain.Base;
using System;

namespace AiVideoStudio.Domain.Events.Auth;

public class UserLoggedOutEvent : IDomainEvent
{
    public string UserId { get; }
    public string IpAddress { get; }
    public DateTimeOffset OccurredOn { get; }

    public UserLoggedOutEvent(string userId, string ipAddress)
    {
        UserId = userId;
        IpAddress = ipAddress;
        OccurredOn = DateTimeOffset.UtcNow;
    }
}

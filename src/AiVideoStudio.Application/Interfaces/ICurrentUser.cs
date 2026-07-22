using System.Collections.Generic;

namespace AiVideoStudio.Application.Interfaces;

public interface ICurrentUser
{
    string? UserId { get; }
    string? Username { get; }
    IEnumerable<string> Roles { get; }
    IEnumerable<string> Permissions { get; }
    bool IsAuthenticated { get; }
    string? CorrelationId { get; }
    string? RequestId { get; }
}

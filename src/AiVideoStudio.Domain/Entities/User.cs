using AiVideoStudio.Domain.Base;
using AiVideoStudio.Domain.Enums;
using System.Collections.Generic;

namespace AiVideoStudio.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public List<string> RoleIds { get; set; } = new();

    // Optimistic Concurrency
    public int Version { get; set; } = 1;

    // Tracking & Audit
    public System.DateTimeOffset? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public int FailedLoginCount { get; set; }
    public System.DateTimeOffset? LockoutUntil { get; set; }
    public System.DateTimeOffset? EmailVerifiedAt { get; set; }
    public System.DateTimeOffset? RevokedAt { get; set; }
    public System.DateTimeOffset? PasswordChangedAt { get; set; }
}

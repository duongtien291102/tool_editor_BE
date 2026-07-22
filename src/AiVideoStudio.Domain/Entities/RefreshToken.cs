using AiVideoStudio.Domain.Base;
using System;

namespace AiVideoStudio.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string JwtId { get; set; } = string.Empty;
    public string FamilyId { get; set; } = string.Empty;
    
    public DateTimeOffset ExpiresAt { get; set; }
    public string CreatedByIp { get; set; } = string.Empty;
    
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? ReasonRevoked { get; set; }
    
    public string? DeviceId { get; set; }
    public string? Browser { get; set; }
    public string? OS { get; set; }
    public string? UserAgent { get; set; }
    public string? IPAddress { get; set; }
    
    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}

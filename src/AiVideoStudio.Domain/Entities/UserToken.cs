using AiVideoStudio.Domain.Base;
using System;

namespace AiVideoStudio.Domain.Entities;

public class UserToken : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string FamilyId { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty; // e.g. "EmailVerification", "PasswordReset"
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public string CreatedByIp { get; set; } = string.Empty;
    public string UsedByIp { get; set; } = string.Empty;
    public DateTimeOffset? RevokedAt { get; set; }
    public string RevokedReason { get; set; } = string.Empty;
}

using AiVideoStudio.Domain.Base;

namespace AiVideoStudio.Domain.Entities.SecurityGovernance;

public sealed class UserSecurityProfile : BaseEntity
{
    public string UserId { get; private set; } = string.Empty;
    public bool MfaEnabled { get; private set; }
    public string? MfaSecret { get; private set; }
    public int PasskeysCount { get; private set; }
    public int ActiveSessionsCount { get; private set; } = 1;
    public double RiskScore { get; private set; } = 10.0; // 0 (low) to 100 (critical)
    public int FailedLoginCount { get; private set; }
    public bool IsLockedOut { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }
    public string? LastKnownIp { get; private set; }

    private UserSecurityProfile() { }

    public static UserSecurityProfile CreateForUser(string userId)
    {
        return new UserSecurityProfile
        {
            UserId = userId,
            MfaEnabled = false,
            RiskScore = 10.0,
            FailedLoginCount = 0,
            IsLockedOut = false,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = userId,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = userId
        };
    }

    public void EnableMfa(string secret)
    {
        MfaEnabled = true;
        MfaSecret = secret;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordFailedLogin(int maxFailures = 5, int lockoutMinutes = 30)
    {
        FailedLoginCount++;
        RiskScore = Math.Min(100.0, RiskScore + 15.0);

        if (FailedLoginCount >= maxFailures)
        {
            IsLockedOut = true;
            LockedUntil = DateTimeOffset.UtcNow.AddMinutes(lockoutMinutes);
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordSuccessfulLogin(string ipAddress)
    {
        FailedLoginCount = 0;
        IsLockedOut = false;
        LockedUntil = null;
        LastKnownIp = ipAddress;
        RiskScore = Math.Max(0.0, RiskScore - 10.0);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateRiskScore(double newScore)
    {
        RiskScore = Math.Clamp(newScore, 0.0, 100.0);
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

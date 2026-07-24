using AiVideoStudio.Domain.Base;

namespace AiVideoStudio.Domain.Entities.SecurityGovernance;

public sealed class TrustedDevice : BaseEntity
{
    public string UserId { get; private set; } = string.Empty;
    public string DeviceFingerprint { get; private set; } = string.Empty;
    public string DeviceName { get; private set; } = string.Empty;
    public string OperatingSystem { get; private set; } = string.Empty;
    public string Browser { get; private set; } = string.Empty;
    public int TrustScore { get; private set; } = 80;
    public bool IsTrusted { get; private set; } = true;
    public DateTimeOffset LastSeenAt { get; private set; } = DateTimeOffset.UtcNow;

    private TrustedDevice() { }

    public static TrustedDevice Register(
        string userId,
        string deviceFingerprint,
        string deviceName,
        string os,
        string browser,
        int trustScore = 80)
    {
        return new TrustedDevice
        {
            UserId = userId,
            DeviceFingerprint = deviceFingerprint,
            DeviceName = deviceName,
            OperatingSystem = os,
            Browser = browser,
            TrustScore = trustScore,
            IsTrusted = trustScore >= 50,
            LastSeenAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = userId,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = userId
        };
    }

    public void UpdateLastSeen(int trustScore)
    {
        TrustScore = trustScore;
        IsTrusted = trustScore >= 50;
        LastSeenAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RevokeTrust(string revokedBy)
    {
        IsTrusted = false;
        TrustScore = 0;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = revokedBy;
    }
}

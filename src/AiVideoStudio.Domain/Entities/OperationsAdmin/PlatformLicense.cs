using AiVideoStudio.Domain.Base;

namespace AiVideoStudio.Domain.Entities.OperationsAdmin;

public sealed class PlatformLicense : BaseEntity
{
    public string TenantId { get; private set; } = string.Empty;
    public string LicenseKey { get; private set; } = string.Empty;
    public string LicenseType { get; private set; } = "Enterprise"; // Starter, Pro, Enterprise
    public int MaxSeats { get; private set; } = 50;
    public int ActiveSeats { get; private set; } = 1;
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Dictionary<string, bool> EnabledFeatures { get; private set; } = new();

    private PlatformLicense() { }

    public static PlatformLicense Create(
        string tenantId,
        string licenseKey,
        string licenseType,
        int maxSeats,
        TimeSpan validityDuration,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(licenseKey)) throw new ArgumentException("LicenseKey is required.", nameof(licenseKey));

        var license = new PlatformLicense
        {
            TenantId = tenantId,
            LicenseKey = licenseKey,
            LicenseType = string.IsNullOrWhiteSpace(licenseType) ? "Enterprise" : licenseType,
            MaxSeats = maxSeats > 0 ? maxSeats : 50,
            ActiveSeats = 1,
            IssuedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(validityDuration > TimeSpan.Zero ? validityDuration : TimeSpan.FromDays(365)),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = createdBy
        };

        license.EnabledFeatures["MultiProviderAi"] = true;
        license.EnabledFeatures["Export4K"] = true;
        license.EnabledFeatures["RealtimeStreaming"] = true;
        return license;
    }

    public void Renew(TimeSpan extension, string updatedBy)
    {
        ExpiresAt = ExpiresAt.Add(extension);
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Revoke(string updatedBy)
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}

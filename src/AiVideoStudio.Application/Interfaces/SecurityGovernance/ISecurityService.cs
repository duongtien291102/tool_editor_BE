using AiVideoStudio.Domain.Entities.SecurityGovernance;

namespace AiVideoStudio.Application.Interfaces.SecurityGovernance;

public record SecurityDashboardSnapshot(
    int TotalIncidents,
    int ActiveThreats,
    double AverageRiskScore,
    int TotalTrustedDevices,
    int ActiveSecretKeys,
    double OverallComplianceScore,
    IReadOnlyList<SecurityIncidentRecord> RecentIncidents);

public interface ISecurityService
{
    Task<SecurityDashboardSnapshot> GetSecurityDashboardSnapshotAsync(CancellationToken cancellationToken = default);
    Task<SecurityPolicy> GetSecurityPolicyAsync(CancellationToken cancellationToken = default);
    Task UpdateSecurityPolicyAsync(bool mfaAdmins, bool mfaHighRisk, int minDeviceTrust, double riskThreshold, int maxFailedLogins, int lockoutMinutes, string updatedBy, CancellationToken cancellationToken = default);
    Task<TrustedDevice> TrustDeviceAsync(string userId, string fingerprint, string deviceName, string os, string browser, int trustScore, CancellationToken cancellationToken = default);
    Task RevokeDeviceAsync(string deviceId, string revokedBy, CancellationToken cancellationToken = default);
}

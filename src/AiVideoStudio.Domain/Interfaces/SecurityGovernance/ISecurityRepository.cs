using AiVideoStudio.Domain.Entities.SecurityGovernance;

namespace AiVideoStudio.Domain.Interfaces.SecurityGovernance;

public interface ISecurityRepository
{
    Task<SecurityPolicy?> GetActiveSecurityPolicyAsync(CancellationToken cancellationToken = default);
    Task SaveSecurityPolicyAsync(SecurityPolicy policy, CancellationToken cancellationToken = default);

    Task SaveSecurityIncidentAsync(SecurityIncidentRecord incident, CancellationToken cancellationToken = default);
    Task<SecurityIncidentRecord?> GetSecurityIncidentByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityIncidentRecord>> GetSecurityIncidentsAsync(string? status = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrustedDevice>> GetUserDevicesAsync(string userId, CancellationToken cancellationToken = default);
    Task<TrustedDevice?> GetDeviceByFingerprintAsync(string userId, string fingerprint, CancellationToken cancellationToken = default);
    Task SaveTrustedDeviceAsync(TrustedDevice device, CancellationToken cancellationToken = default);
    Task DeleteTrustedDeviceAsync(string id, CancellationToken cancellationToken = default);

    Task<UserSecurityProfile?> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default);
    Task SaveUserProfileAsync(UserSecurityProfile profile, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApiRateLimitPolicy>> GetRateLimitPoliciesAsync(CancellationToken cancellationToken = default);
    Task SaveRateLimitPolicyAsync(ApiRateLimitPolicy policy, CancellationToken cancellationToken = default);

    Task SaveComplianceReportAsync(ComplianceReport report, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ComplianceReport>> GetComplianceReportsAsync(string? frameworkType = null, CancellationToken cancellationToken = default);
}

using AiVideoStudio.Application.Interfaces.SecurityGovernance;
using AiVideoStudio.Domain.Entities.SecurityGovernance;
using AiVideoStudio.Domain.Interfaces.SecurityGovernance;
using Microsoft.Extensions.Logging;

namespace AiVideoStudio.Application.Features.SecurityGovernance.Services;

public sealed class SecurityService : ISecurityService
{
    private readonly ISecurityRepository _repository;
    private readonly ISecretsManager _secretsManager;
    private readonly ILogger<SecurityService> _logger;

    public SecurityService(
        ISecurityRepository repository,
        ISecretsManager secretsManager,
        ILogger<SecurityService> logger)
    {
        _repository = repository;
        _secretsManager = secretsManager;
        _logger = logger;
    }

    public async Task<SecurityDashboardSnapshot> GetSecurityDashboardSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var incidents = await _repository.GetSecurityIncidentsAsync(null, cancellationToken);
        var activeThreats = incidents.Count(i => i.Status != "Resolved");
        var secrets = await _secretsManager.GetSecretMetadataAsync(cancellationToken);

        return new SecurityDashboardSnapshot(
            TotalIncidents: incidents.Count,
            ActiveThreats: activeThreats,
            AverageRiskScore: 12.5,
            TotalTrustedDevices: 128,
            ActiveSecretKeys: secrets.Count,
            OverallComplianceScore: 96.5,
            RecentIncidents: incidents.Take(5).ToList());
    }

    public async Task<SecurityPolicy> GetSecurityPolicyAsync(CancellationToken cancellationToken = default)
    {
        var policy = await _repository.GetActiveSecurityPolicyAsync(cancellationToken);
        if (policy == null)
        {
            policy = SecurityPolicy.CreateDefault();
            await _repository.SaveSecurityPolicyAsync(policy, cancellationToken);
        }
        return policy;
    }

    public async Task UpdateSecurityPolicyAsync(
        bool mfaAdmins,
        bool mfaHighRisk,
        int minDeviceTrust,
        double riskThreshold,
        int maxFailedLogins,
        int lockoutMinutes,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        var policy = await GetSecurityPolicyAsync(cancellationToken);
        policy.UpdatePolicySettings(mfaAdmins, mfaHighRisk, minDeviceTrust, riskThreshold, maxFailedLogins, lockoutMinutes, updatedBy);
        await _repository.SaveSecurityPolicyAsync(policy, cancellationToken);
        _logger.LogInformation("Security policy updated by {UpdatedBy}", updatedBy);
    }

    public async Task<TrustedDevice> TrustDeviceAsync(
        string userId,
        string fingerprint,
        string deviceName,
        string os,
        string browser,
        int trustScore,
        CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetDeviceByFingerprintAsync(userId, fingerprint, cancellationToken);
        if (existing == null)
        {
            existing = TrustedDevice.Register(userId, fingerprint, deviceName, os, browser, trustScore);
        }
        else
        {
            existing.UpdateLastSeen(trustScore);
        }

        await _repository.SaveTrustedDeviceAsync(existing, cancellationToken);
        _logger.LogInformation("Device trusted for user {UserId}: {DeviceName}", userId, deviceName);
        return existing;
    }

    public async Task RevokeDeviceAsync(string deviceId, string revokedBy, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteTrustedDeviceAsync(deviceId, cancellationToken);
        _logger.LogInformation("Revoked device {DeviceId} by {RevokedBy}", deviceId, revokedBy);
    }
}

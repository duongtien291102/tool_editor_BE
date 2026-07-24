using AiVideoStudio.Application.Interfaces.SecurityGovernance;
using AiVideoStudio.Domain.Interfaces.SecurityGovernance;

namespace AiVideoStudio.Application.Features.SecurityGovernance.Services;

public sealed class RiskEngine : IRiskEngine
{
    private readonly ISecurityRepository _repository;

    public RiskEngine(ISecurityRepository repository)
    {
        _repository = repository;
    }

    public async Task<RiskAssessmentResult> CalculateRiskAsync(
        string userId,
        string clientIp,
        string userAgent,
        string? deviceFingerprint,
        CancellationToken cancellationToken = default)
    {
        double score = 10.0;
        var factors = new List<string>();

        var userProfile = await _repository.GetUserProfileAsync(userId, cancellationToken);
        if (userProfile != null)
        {
            if (userProfile.FailedLoginCount > 2)
            {
                score += userProfile.FailedLoginCount * 10;
                factors.Add($"Recent failed login attempts: {userProfile.FailedLoginCount}");
            }
            if (userProfile.LastKnownIp != null && userProfile.LastKnownIp != clientIp)
            {
                score += 20;
                factors.Add($"New IP address detected ({clientIp})");
            }
        }

        if (!string.IsNullOrWhiteSpace(deviceFingerprint))
        {
            var device = await _repository.GetDeviceByFingerprintAsync(userId, deviceFingerprint, cancellationToken);
            if (device == null || !device.IsTrusted)
            {
                score += 25;
                factors.Add("Untrusted or unknown device fingerprint");
            }
        }
        else
        {
            score += 15;
            factors.Add("Missing device fingerprint");
        }

        score = Math.Clamp(score, 0.0, 100.0);
        string level = score switch
        {
            >= 75 => "Critical",
            >= 50 => "High",
            >= 25 => "Medium",
            _ => "Low"
        };

        bool requiresMfa = score >= 50;
        return new RiskAssessmentResult(score, level, requiresMfa, factors);
    }
}

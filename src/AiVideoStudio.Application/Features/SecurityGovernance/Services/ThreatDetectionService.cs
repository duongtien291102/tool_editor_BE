using AiVideoStudio.Application.Interfaces.SecurityGovernance;
using AiVideoStudio.Domain.Entities.SecurityGovernance;
using AiVideoStudio.Domain.Interfaces.SecurityGovernance;
using Microsoft.Extensions.Logging;

namespace AiVideoStudio.Application.Features.SecurityGovernance.Services;

public sealed class ThreatDetectionService : IThreatDetectionService
{
    private readonly ISecurityRepository _repository;
    private readonly ILogger<ThreatDetectionService> _logger;

    public ThreatDetectionService(
        ISecurityRepository repository,
        ILogger<ThreatDetectionService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task AnalyzeActivityAsync(string userId, string ipAddress, string userAgent, string action, CancellationToken cancellationToken = default)
    {
        if (action == "LOGIN_FAILED")
        {
            var profile = await _repository.GetUserProfileAsync(userId, cancellationToken) ?? UserSecurityProfile.CreateForUser(userId);
            profile.RecordFailedLogin();
            await _repository.SaveUserProfileAsync(profile, cancellationToken);

            if (profile.FailedLoginCount >= 5)
            {
                var incident = SecurityIncidentRecord.Create(
                    title: $"Potential Brute Force Attack detected for User {userId}",
                    threatType: "BruteForce",
                    severity: "High",
                    sourceIp: ipAddress,
                    targetUserId: userId,
                    createdBy: "ThreatDetectionService");

                incident.AddSignal("FailedLoginBurst", $"5 consecutive failed logins from IP {ipAddress}", 0.95);
                await _repository.SaveSecurityIncidentAsync(incident, cancellationToken);
                _logger.LogWarning("Brute Force threat incident filed for user {User} from IP {Ip}", userId, ipAddress);
            }
        }
    }

    public async Task<IReadOnlyList<SecurityIncidentRecord>> GetIncidentsAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        return await _repository.GetSecurityIncidentsAsync(status, cancellationToken);
    }
}

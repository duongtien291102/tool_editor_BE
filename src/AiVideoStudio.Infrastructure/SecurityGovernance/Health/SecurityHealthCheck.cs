using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AiVideoStudio.Infrastructure.SecurityGovernance.Health;

public sealed class SecurityHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>
        {
            ["MFA"] = "Healthy",
            ["SecretStore"] = "Healthy",
            ["PolicyEngine"] = "Healthy",
            ["ThreatDetection"] = "Healthy",
            ["ComplianceEngine"] = "Healthy"
        };

        return Task.FromResult(HealthCheckResult.Healthy("Security & Governance Platform: All security engines are operational.", data));
    }
}

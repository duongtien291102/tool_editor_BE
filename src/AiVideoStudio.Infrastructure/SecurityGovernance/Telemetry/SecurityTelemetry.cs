using System.Diagnostics.Metrics;

namespace AiVideoStudio.Infrastructure.SecurityGovernance.Telemetry;

public sealed class SecurityTelemetry
{
    public static readonly string MeterName = "AiVideoStudio.SecurityGovernance";
    private static readonly Meter Meter = new(MeterName, "1.0.0");

    public Counter<long> SecurityEventsTotal { get; }
    public Counter<long> SecurityIncidentsTotal { get; }
    public Counter<long> FailedLoginTotal { get; }
    public Counter<long> MfaSuccessTotal { get; }
    public Counter<long> MfaFailureTotal { get; }
    public UpDownCounter<double> RiskScoreAverage { get; }
    public Counter<long> BlockedRequestsTotal { get; }
    public Counter<long> RateLimitHits { get; }
    public UpDownCounter<long> DeviceTrustTotal { get; }
    public Counter<long> SecretRotationsTotal { get; }

    public SecurityTelemetry()
    {
        SecurityEventsTotal = Meter.CreateCounter<long>("security_events_total", "{event}", "Total security events processed");
        SecurityIncidentsTotal = Meter.CreateCounter<long>("security_incidents_total", "{incident}", "Total security incidents recorded");
        FailedLoginTotal = Meter.CreateCounter<long>("failed_login_total", "{attempt}", "Total failed login attempts");
        MfaSuccessTotal = Meter.CreateCounter<long>("mfa_success_total", "{attempt}", "Total MFA verification successes");
        MfaFailureTotal = Meter.CreateCounter<long>("mfa_failure_total", "{attempt}", "Total MFA verification failures");
        RiskScoreAverage = Meter.CreateUpDownCounter<double>("risk_score_average", "score", "Average user risk score");
        BlockedRequestsTotal = Meter.CreateCounter<long>("blocked_requests_total", "{request}", "Total blocked requests");
        RateLimitHits = Meter.CreateCounter<long>("rate_limit_hits", "{hit}", "Total API rate limit hits");
        DeviceTrustTotal = Meter.CreateUpDownCounter<long>("device_trust_total", "{device}", "Total trusted devices registered");
        SecretRotationsTotal = Meter.CreateCounter<long>("secret_rotations_total", "{rotation}", "Total secret key rotations executed");
    }
}

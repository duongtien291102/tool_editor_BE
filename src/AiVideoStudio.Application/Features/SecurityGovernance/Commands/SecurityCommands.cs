using AiVideoStudio.Application.Interfaces.SecurityGovernance;
using AiVideoStudio.Domain.Entities.SecurityGovernance;
using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.SecurityGovernance.Commands;

public record GetSecurityDashboardQuery : IRequest<Result<SecurityDashboardSnapshot>>;

public record GetSecurityPolicyQuery : IRequest<Result<SecurityPolicy>>;

public record UpdateSecurityPolicyCommand(
    bool MfaRequiredForAdmins,
    bool MfaRequiredForHighRisk,
    int MinDeviceTrustScore,
    double HighRiskThreshold,
    int MaxFailedLoginsBeforeLockout,
    int LockoutDurationMinutes,
    string UpdatedBy) : IRequest<Result<Unit>>;

public record CreateSecurityIncidentCommand(
    string Title,
    string ThreatType,
    string Severity,
    string SourceIp,
    string? TargetUserId,
    string CreatedBy) : IRequest<Result<SecurityIncidentRecord>>;

public record GetSecurityIncidentsQuery(string? Status = null) : IRequest<Result<IReadOnlyList<SecurityIncidentRecord>>>;

public record GenerateComplianceReportCommand(
    string FrameworkType,
    string GeneratedBy) : IRequest<Result<ComplianceReport>>;

public record GetComplianceReportsQuery(string? FrameworkType = null) : IRequest<Result<IReadOnlyList<ComplianceReport>>>;

public record AssessRiskQuery(
    string UserId,
    string ClientIp,
    string UserAgent,
    string? DeviceFingerprint) : IRequest<Result<RiskAssessmentResult>>;

public record TrustDeviceCommand(
    string UserId,
    string DeviceFingerprint,
    string DeviceName,
    string OperatingSystem,
    string Browser,
    int TrustScore) : IRequest<Result<TrustedDevice>>;

public record RevokeDeviceCommand(
    string DeviceId,
    string RevokedBy) : IRequest<Result<Unit>>;

public record RotateSecretCommand(
    string KeyName,
    string RotatedBy) : IRequest<Result<SecretKeyMetadata>>;

public record GetSecretMetadataQuery : IRequest<Result<IReadOnlyList<SecretKeyMetadata>>>;

public record GetRateLimitPoliciesQuery : IRequest<Result<IReadOnlyList<ApiRateLimitPolicy>>>;

public record UpdateRateLimitPolicyCommand(
    string EndpointPath,
    int BurstCapacity,
    int SustainedRatePerMinute,
    int WindowSeconds,
    bool IsEnabled,
    string UpdatedBy) : IRequest<Result<Unit>>;

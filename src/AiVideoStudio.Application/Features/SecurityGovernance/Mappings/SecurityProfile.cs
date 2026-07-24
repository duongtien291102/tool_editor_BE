using AutoMapper;
using AiVideoStudio.Domain.Entities.SecurityGovernance;

namespace AiVideoStudio.Application.Features.SecurityGovernance.Mappings;

public record SecurityPolicyDto(
    string Id,
    string Name,
    string Description,
    bool MfaRequiredForAdmins,
    bool MfaRequiredForHighRisk,
    int MinDeviceTrustScore,
    double HighRiskThreshold,
    int MaxFailedLoginsBeforeLockout,
    int LockoutDurationMinutes);

public record SecurityIncidentDto(
    string Id,
    string Title,
    string ThreatType,
    string Severity,
    string Status,
    string SourceIp,
    string? TargetUserId,
    DateTimeOffset CreatedAt);

public record TrustedDeviceDto(
    string Id,
    string UserId,
    string DeviceFingerprint,
    string DeviceName,
    int TrustScore,
    bool IsTrusted,
    DateTimeOffset LastSeenAt);

public sealed class SecurityProfile : Profile
{
    public SecurityProfile()
    {
        CreateMap<SecurityPolicy, SecurityPolicyDto>();
        CreateMap<SecurityIncidentRecord, SecurityIncidentDto>();
        CreateMap<TrustedDevice, TrustedDeviceDto>();
    }
}

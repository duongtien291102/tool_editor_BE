using AiVideoStudio.Application.Features.SecurityGovernance.Commands;
using AiVideoStudio.Application.Interfaces.SecurityGovernance;
using AiVideoStudio.Domain.Entities.SecurityGovernance;
using AiVideoStudio.Domain.Interfaces.SecurityGovernance;
using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.SecurityGovernance.Handlers;

public sealed class SecurityHandlers :
    IRequestHandler<GetSecurityDashboardQuery, Result<SecurityDashboardSnapshot>>,
    IRequestHandler<GetSecurityPolicyQuery, Result<SecurityPolicy>>,
    IRequestHandler<UpdateSecurityPolicyCommand, Result<Unit>>,
    IRequestHandler<CreateSecurityIncidentCommand, Result<SecurityIncidentRecord>>,
    IRequestHandler<GetSecurityIncidentsQuery, Result<IReadOnlyList<SecurityIncidentRecord>>>,
    IRequestHandler<GenerateComplianceReportCommand, Result<ComplianceReport>>,
    IRequestHandler<GetComplianceReportsQuery, Result<IReadOnlyList<ComplianceReport>>>,
    IRequestHandler<AssessRiskQuery, Result<RiskAssessmentResult>>,
    IRequestHandler<TrustDeviceCommand, Result<TrustedDevice>>,
    IRequestHandler<RevokeDeviceCommand, Result<Unit>>,
    IRequestHandler<RotateSecretCommand, Result<SecretKeyMetadata>>,
    IRequestHandler<GetSecretMetadataQuery, Result<IReadOnlyList<SecretKeyMetadata>>>,
    IRequestHandler<GetRateLimitPoliciesQuery, Result<IReadOnlyList<ApiRateLimitPolicy>>>,
    IRequestHandler<UpdateRateLimitPolicyCommand, Result<Unit>>
{
    private readonly ISecurityService _securityService;
    private readonly IThreatDetectionService _threatDetectionService;
    private readonly IRiskEngine _riskEngine;
    private readonly ISecretsManager _secretsManager;
    private readonly IComplianceService _complianceService;
    private readonly ISecurityRepository _securityRepository;

    public SecurityHandlers(
        ISecurityService securityService,
        IThreatDetectionService threatDetectionService,
        IRiskEngine riskEngine,
        ISecretsManager secretsManager,
        IComplianceService complianceService,
        ISecurityRepository securityRepository)
    {
        _securityService = securityService;
        _threatDetectionService = threatDetectionService;
        _riskEngine = riskEngine;
        _secretsManager = secretsManager;
        _complianceService = complianceService;
        _securityRepository = securityRepository;
    }

    public async Task<Result<SecurityDashboardSnapshot>> Handle(GetSecurityDashboardQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await _securityService.GetSecurityDashboardSnapshotAsync(cancellationToken);
        return Result<SecurityDashboardSnapshot>.Success(snapshot);
    }

    public async Task<Result<SecurityPolicy>> Handle(GetSecurityPolicyQuery request, CancellationToken cancellationToken)
    {
        var policy = await _securityService.GetSecurityPolicyAsync(cancellationToken);
        return Result<SecurityPolicy>.Success(policy);
    }

    public async Task<Result<Unit>> Handle(UpdateSecurityPolicyCommand request, CancellationToken cancellationToken)
    {
        await _securityService.UpdateSecurityPolicyAsync(
            request.MfaRequiredForAdmins,
            request.MfaRequiredForHighRisk,
            request.MinDeviceTrustScore,
            request.HighRiskThreshold,
            request.MaxFailedLoginsBeforeLockout,
            request.LockoutDurationMinutes,
            request.UpdatedBy,
            cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }

    public async Task<Result<SecurityIncidentRecord>> Handle(CreateSecurityIncidentCommand request, CancellationToken cancellationToken)
    {
        var incident = SecurityIncidentRecord.Create(
            request.Title,
            request.ThreatType,
            request.Severity,
            request.SourceIp,
            request.TargetUserId,
            request.CreatedBy);

        await _securityRepository.SaveSecurityIncidentAsync(incident, cancellationToken);
        return Result<SecurityIncidentRecord>.Success(incident);
    }

    public async Task<Result<IReadOnlyList<SecurityIncidentRecord>>> Handle(GetSecurityIncidentsQuery request, CancellationToken cancellationToken)
    {
        var incidents = await _threatDetectionService.GetIncidentsAsync(request.Status, cancellationToken);
        return Result<IReadOnlyList<SecurityIncidentRecord>>.Success(incidents);
    }

    public async Task<Result<ComplianceReport>> Handle(GenerateComplianceReportCommand request, CancellationToken cancellationToken)
    {
        var report = await _complianceService.GenerateReportAsync(request.FrameworkType, request.GeneratedBy, cancellationToken);
        return Result<ComplianceReport>.Success(report);
    }

    public async Task<Result<IReadOnlyList<ComplianceReport>>> Handle(GetComplianceReportsQuery request, CancellationToken cancellationToken)
    {
        var reports = await _complianceService.GetReportsAsync(request.FrameworkType, cancellationToken);
        return Result<IReadOnlyList<ComplianceReport>>.Success(reports);
    }

    public async Task<Result<RiskAssessmentResult>> Handle(AssessRiskQuery request, CancellationToken cancellationToken)
    {
        var result = await _riskEngine.CalculateRiskAsync(request.UserId, request.ClientIp, request.UserAgent, request.DeviceFingerprint, cancellationToken);
        return Result<RiskAssessmentResult>.Success(result);
    }

    public async Task<Result<TrustedDevice>> Handle(TrustDeviceCommand request, CancellationToken cancellationToken)
    {
        var device = await _securityService.TrustDeviceAsync(request.UserId, request.DeviceFingerprint, request.DeviceName, request.OperatingSystem, request.Browser, request.TrustScore, cancellationToken);
        return Result<TrustedDevice>.Success(device);
    }

    public async Task<Result<Unit>> Handle(RevokeDeviceCommand request, CancellationToken cancellationToken)
    {
        await _securityService.RevokeDeviceAsync(request.DeviceId, request.RevokedBy, cancellationToken);
        return Result<Unit>.Success(Unit.Value);
    }

    public async Task<Result<SecretKeyMetadata>> Handle(RotateSecretCommand request, CancellationToken cancellationToken)
    {
        var metadata = await _secretsManager.RotateSecretAsync(request.KeyName, request.RotatedBy, cancellationToken);
        return Result<SecretKeyMetadata>.Success(metadata);
    }

    public async Task<Result<IReadOnlyList<SecretKeyMetadata>>> Handle(GetSecretMetadataQuery request, CancellationToken cancellationToken)
    {
        var list = await _secretsManager.GetSecretMetadataAsync(cancellationToken);
        return Result<IReadOnlyList<SecretKeyMetadata>>.Success(list);
    }

    public async Task<Result<IReadOnlyList<ApiRateLimitPolicy>>> Handle(GetRateLimitPoliciesQuery request, CancellationToken cancellationToken)
    {
        var policies = await _securityRepository.GetRateLimitPoliciesAsync(cancellationToken);
        return Result<IReadOnlyList<ApiRateLimitPolicy>>.Success(policies);
    }

    public async Task<Result<Unit>> Handle(UpdateRateLimitPolicyCommand request, CancellationToken cancellationToken)
    {
        var existing = (await _securityRepository.GetRateLimitPoliciesAsync(cancellationToken)).FirstOrDefault(p => p.EndpointPath == request.EndpointPath);
        if (existing == null)
        {
            existing = ApiRateLimitPolicy.Create(request.EndpointPath, request.BurstCapacity, request.SustainedRatePerMinute, request.WindowSeconds, request.UpdatedBy);
        }
        else
        {
            existing.UpdatePolicy(request.BurstCapacity, request.SustainedRatePerMinute, request.WindowSeconds, request.IsEnabled, request.UpdatedBy);
        }

        await _securityRepository.SaveRateLimitPolicyAsync(existing, cancellationToken);
        return Result<Unit>.Success(Unit.Value);
    }
}

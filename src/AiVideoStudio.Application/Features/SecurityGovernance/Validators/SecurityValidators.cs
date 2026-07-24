using AiVideoStudio.Application.Features.SecurityGovernance.Commands;
using FluentValidation;

namespace AiVideoStudio.Application.Features.SecurityGovernance.Validators;

public sealed class UpdateSecurityPolicyValidator : AbstractValidator<UpdateSecurityPolicyCommand>
{
    public UpdateSecurityPolicyValidator()
    {
        RuleFor(x => x.MinDeviceTrustScore).InclusiveBetween(0, 100);
        RuleFor(x => x.HighRiskThreshold).InclusiveBetween(0.0, 100.0);
        RuleFor(x => x.MaxFailedLoginsBeforeLockout).GreaterThan(0);
        RuleFor(x => x.LockoutDurationMinutes).GreaterThan(0);
        RuleFor(x => x.UpdatedBy).NotEmpty();
    }
}

public sealed class CreateSecurityIncidentValidator : AbstractValidator<CreateSecurityIncidentCommand>
{
    public CreateSecurityIncidentValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ThreatType).NotEmpty();
        RuleFor(x => x.Severity).NotEmpty();
        RuleFor(x => x.SourceIp).NotEmpty();
        RuleFor(x => x.CreatedBy).NotEmpty();
    }
}

public sealed class TrustDeviceValidator : AbstractValidator<TrustDeviceCommand>
{
    public TrustDeviceValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DeviceFingerprint).NotEmpty();
        RuleFor(x => x.DeviceName).NotEmpty();
    }
}

using AiVideoStudio.Application.Features.OperationsAdmin.Commands;
using FluentValidation;

namespace AiVideoStudio.Application.Features.OperationsAdmin.Validators;

public sealed class UpdatePlatformConfigurationValidator : AbstractValidator<UpdatePlatformConfigurationCommand>
{
    public UpdatePlatformConfigurationValidator()
    {
        RuleFor(x => x.RetentionDays).GreaterThan(0);
        RuleFor(x => x.MaxConcurrentJobsPerTenant).GreaterThan(0);
        RuleFor(x => x.MaxDailyExportsPerUser).GreaterThan(0);
        RuleFor(x => x.UpdatedBy).NotEmpty();
    }
}

public sealed class CreateIncidentValidator : AbstractValidator<CreateIncidentCommand>
{
    public CreateIncidentValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Severity).NotEmpty();
        RuleFor(x => x.CreatedBy).NotEmpty();
    }
}

public sealed class ScheduleMaintenanceValidator : AbstractValidator<ScheduleMaintenanceCommand>
{
    public ScheduleMaintenanceValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.ScheduledStart).NotEmpty();
        RuleFor(x => x.ScheduledEnd).GreaterThan(x => x.ScheduledStart);
        RuleFor(x => x.CreatedBy).NotEmpty();
    }
}

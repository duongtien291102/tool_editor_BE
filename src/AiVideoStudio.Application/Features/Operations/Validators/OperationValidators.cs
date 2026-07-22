using FluentValidation;
namespace AiVideoStudio.Application.Features.Operations.Validators;
public sealed class CreateNotificationValidator:AbstractValidator<CreateNotificationCommand>{public CreateNotificationValidator(){RuleFor(x=>x.UserId).NotEmpty();RuleFor(x=>x.Title).NotEmpty().MaximumLength(200);RuleFor(x=>x.Message).NotEmpty().MaximumLength(4000);RuleFor(x=>x.Type).IsInEnum();}}
public sealed class MarkNotificationReadValidator:AbstractValidator<MarkNotificationReadCommand>{public MarkNotificationReadValidator()=>RuleFor(x=>x.Id).NotEmpty();}
public sealed class UpdateSystemConfigurationValidator:AbstractValidator<UpdateSystemConfigurationCommand>{public UpdateSystemConfigurationValidator(){RuleFor(x=>x.Key).NotEmpty().MaximumLength(200);RuleFor(x=>x.Value).NotNull().MaximumLength(8000);}}
public sealed class RecordUsageValidator:AbstractValidator<RecordUsageCommand>{public RecordUsageValidator(){RuleFor(x=>x.Type).IsInEnum();RuleFor(x=>x.Amount).GreaterThan(0);RuleFor(x=>x.ResourceId).NotEmpty();}}
public sealed class RunMaintenanceValidator:AbstractValidator<RunMaintenanceCommand>{public RunMaintenanceValidator()=>RuleFor(x=>x.Name).NotEmpty().MaximumLength(200);}
public sealed class GetNotificationsValidator:AbstractValidator<GetNotificationsQuery>{public GetNotificationsValidator(){RuleFor(x=>x.Page).GreaterThan(0);RuleFor(x=>x.PageSize).InclusiveBetween(1,100);}}
public sealed class GetAuditLogsValidator:AbstractValidator<GetAuditLogsQuery>{public GetAuditLogsValidator(){RuleFor(x=>x.Page).GreaterThan(0);RuleFor(x=>x.PageSize).InclusiveBetween(1,100);}}

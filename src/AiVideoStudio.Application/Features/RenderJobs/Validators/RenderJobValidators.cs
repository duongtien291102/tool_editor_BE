using FluentValidation;

namespace AiVideoStudio.Application.Features.RenderJobs.Validators;

public class CreateRenderJobCommandValidator : AbstractValidator<CreateRenderJobCommand>
{
    public CreateRenderJobCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("ProjectId is required.");

        RuleFor(x => x.MaxRetryCount)
            .GreaterThanOrEqualTo(0).WithMessage("MaxRetryCount must be >= 0.")
            .LessThanOrEqualTo(10).WithMessage("MaxRetryCount cannot exceed 10.");

        RuleFor(x => x.JobType)
            .IsInEnum().WithMessage("Invalid JobType.");

        RuleFor(x => x.Provider)
            .IsInEnum().WithMessage("Invalid Provider.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid Priority.");
    }
}

public class CancelRenderJobCommandValidator : AbstractValidator<CancelRenderJobCommand>
{
    public CancelRenderJobCommandValidator()
    {
        RuleFor(x => x.JobId)
            .NotEmpty().WithMessage("JobId is required.");
    }
}

public class RetryRenderJobCommandValidator : AbstractValidator<RetryRenderJobCommand>
{
    public RetryRenderJobCommandValidator()
    {
        RuleFor(x => x.JobId)
            .NotEmpty().WithMessage("JobId is required.");
    }
}

public class UpdateRenderProgressCommandValidator : AbstractValidator<UpdateRenderProgressCommand>
{
    public UpdateRenderProgressCommandValidator()
    {
        RuleFor(x => x.JobId)
            .NotEmpty().WithMessage("JobId is required.");

        RuleFor(x => x.Progress)
            .InclusiveBetween(0, 100).WithMessage("Progress must be between 0 and 100.");
    }
}

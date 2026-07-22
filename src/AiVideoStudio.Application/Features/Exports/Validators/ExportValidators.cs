using FluentValidation;

namespace AiVideoStudio.Application.Features.Exports.Validators;

public sealed class CreateExportJobValidator : AbstractValidator<CreateExportJobCommand>
{
    public CreateExportJobValidator()
    {
        RuleFor(x => x.RenderJobId).NotEmpty();
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.TimelineId).NotEmpty();
        RuleFor(x => x.VideoCodec).IsInEnum();
        RuleFor(x => x.AudioCodec).IsInEnum();
        RuleFor(x => x.Container).IsInEnum();
        RuleFor(x => x.MaxRetryCount).InclusiveBetween(0, 10);
    }
}

public sealed class RetryExportJobValidator : AbstractValidator<RetryExportJobCommand>
{
    public RetryExportJobValidator() => RuleFor(x => x.ExportJobId).NotEmpty();
}

public sealed class CancelExportJobValidator : AbstractValidator<CancelExportJobCommand>
{
    public CancelExportJobValidator() => RuleFor(x => x.ExportJobId).NotEmpty();
}

public sealed class UpdateExportProgressValidator : AbstractValidator<UpdateExportProgressCommand>
{
    public UpdateExportProgressValidator()
    {
        RuleFor(x => x.ExportJobId).NotEmpty();
        RuleFor(x => x.Progress).InclusiveBetween(0, 99);
    }
}

public sealed class GetExportJobValidator : AbstractValidator<GetExportJobQuery>
{
    public GetExportJobValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class GetProjectExportJobsValidator : AbstractValidator<GetProjectExportJobsQuery>
{
    public GetProjectExportJobsValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

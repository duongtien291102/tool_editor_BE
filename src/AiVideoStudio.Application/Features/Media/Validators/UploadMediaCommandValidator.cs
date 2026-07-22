using AiVideoStudio.Application.Features.Media.Commands;
using FluentValidation;

namespace AiVideoStudio.Application.Features.Media.Validators;

public class UploadMediaCommandValidator : AbstractValidator<UploadMediaCommand>
{
    public UploadMediaCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("ProjectId is required.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("FileName is required.")
            .MaximumLength(255).WithMessage("FileName must not exceed 255 characters.");

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("FileSize must be greater than zero.");

        RuleFor(x => x.ContentStream)
            .NotNull().WithMessage("ContentStream cannot be null.");
    }
}

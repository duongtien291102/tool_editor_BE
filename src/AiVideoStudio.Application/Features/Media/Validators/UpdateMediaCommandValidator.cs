using AiVideoStudio.Application.Features.Media.Commands;
using FluentValidation;

namespace AiVideoStudio.Application.Features.Media.Validators;

public class UpdateMediaCommandValidator : AbstractValidator<UpdateMediaCommand>
{
    public UpdateMediaCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Media ID is required.");

        RuleFor(x => x.FileName)
            .MaximumLength(255).WithMessage("FileName must not exceed 255 characters.")
            .When(x => !string.IsNullOrEmpty(x.FileName));

        RuleFor(x => x.Width)
            .GreaterThan(0).WithMessage("Width must be greater than zero.")
            .When(x => x.Width.HasValue);

        RuleFor(x => x.Height)
            .GreaterThan(0).WithMessage("Height must be greater than zero.")
            .When(x => x.Height.HasValue);

        RuleFor(x => x.Duration)
            .GreaterThanOrEqualTo(0).WithMessage("Duration must be non-negative.")
            .When(x => x.Duration.HasValue);
    }
}

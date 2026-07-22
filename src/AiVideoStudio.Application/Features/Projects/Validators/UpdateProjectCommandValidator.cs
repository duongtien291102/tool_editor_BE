using AiVideoStudio.Application.Features.Projects.Commands;
using FluentValidation;

namespace AiVideoStudio.Application.Features.Projects.Validators;

public class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(200).WithMessage("Project name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.Thumbnail)
            .MaximumLength(1000).WithMessage("Thumbnail URL must not exceed 1000 characters.");
    }
}

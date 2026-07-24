using AiVideoStudio.Application.Features.Orchestration.Commands;
using AiVideoStudio.Application.Features.Orchestration.DTOs;
using FluentValidation;

namespace AiVideoStudio.Application.Features.Orchestration.Validators;

public sealed class CreateGenerationWorkflowValidator : AbstractValidator<CreateGenerationWorkflowCommand>
{
    public CreateGenerationWorkflowValidator()
    {
        RuleFor(x => x.Request.ProjectId).NotEmpty().WithMessage("ProjectId is required.");
        RuleFor(x => x.Request.Name).NotEmpty().WithMessage("Workflow name is required.");
        RuleFor(x => x.Request.Steps).NotEmpty().WithMessage("At least one step is required in workflow.");
        RuleForEach(x => x.Request.Steps).SetValidator(new CreateOrchestrationStepValidator());
    }
}

public sealed class CreateOrchestrationStepValidator : AbstractValidator<CreateOrchestrationStepDto>
{
    public CreateOrchestrationStepValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Step name is required.");
        RuleFor(x => x.TimeoutSeconds).GreaterThan(0).WithMessage("TimeoutSeconds must be greater than 0.");
        RuleFor(x => x.MaxRetries).GreaterThanOrEqualTo(0).WithMessage("MaxRetries must be greater than or equal to 0.");
    }
}

using AiVideoStudio.Application.Features.Orchestration.DTOs;
using AiVideoStudio.Domain.Entities.Orchestration;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.Orchestration.Commands;

public record CreateGenerationWorkflowCommand(
    CreateGenerationWorkflowRequest Request,
    string OwnerId) : IRequest<Result<GenerationWorkflowDto>>;

public record QueueGenerationWorkflowCommand(string WorkflowId) : IRequest<Result<GenerationWorkflowDto>>;

public record RunGenerationWorkflowCommand(string WorkflowId) : IRequest<Result<WorkflowResult>>;

public record RetryGenerationWorkflowCommand(string WorkflowId, string? StepId = null) : IRequest<Result<GenerationWorkflowDto>>;

public record ResumeGenerationWorkflowCommand(string WorkflowId) : IRequest<Result<GenerationWorkflowDto>>;

public record CancelGenerationWorkflowCommand(string WorkflowId, string Reason = "User requested cancellation.") : IRequest<Result>;

public record GetGenerationWorkflowQuery(string WorkflowId) : IRequest<Result<GenerationWorkflowDto>>;

public record GetGenerationWorkflowStatusQuery(string WorkflowId) : IRequest<Result<WorkflowState>>;

public record GetGenerationWorkflowHistoryQuery(string WorkflowId) : IRequest<Result<List<WorkflowHistoryDto>>>;

public record GetProjectGenerationWorkflowsQuery(string ProjectId, int Page = 1, int PageSize = 20) : IRequest<Result<PagedResult<GenerationWorkflowDto>>>;

using AiVideoStudio.Application.Features.Orchestration.Commands;
using AiVideoStudio.Application.Features.Orchestration.DTOs;
using AiVideoStudio.Application.Interfaces.Orchestration;
using AiVideoStudio.Domain.Entities.Orchestration;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces.Orchestration;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using AutoMapper;
using MediatR;

namespace AiVideoStudio.Application.Features.Orchestration.Handlers;

public sealed class OrchestrationHandlers :
    IRequestHandler<CreateGenerationWorkflowCommand, Result<GenerationWorkflowDto>>,
    IRequestHandler<QueueGenerationWorkflowCommand, Result<GenerationWorkflowDto>>,
    IRequestHandler<RunGenerationWorkflowCommand, Result<WorkflowResult>>,
    IRequestHandler<RetryGenerationWorkflowCommand, Result<GenerationWorkflowDto>>,
    IRequestHandler<ResumeGenerationWorkflowCommand, Result<GenerationWorkflowDto>>,
    IRequestHandler<CancelGenerationWorkflowCommand, Result>,
    IRequestHandler<GetGenerationWorkflowQuery, Result<GenerationWorkflowDto>>,
    IRequestHandler<GetGenerationWorkflowStatusQuery, Result<WorkflowState>>,
    IRequestHandler<GetGenerationWorkflowHistoryQuery, Result<List<WorkflowHistoryDto>>>,
    IRequestHandler<GetProjectGenerationWorkflowsQuery, Result<PagedResult<GenerationWorkflowDto>>>
{
    private readonly IGenerationOrchestrator _orchestrator;
    private readonly IGenerationWorkflowRepository _repository;
    private readonly IMapper _mapper;

    public OrchestrationHandlers(
        IGenerationOrchestrator orchestrator,
        IGenerationWorkflowRepository repository,
        IMapper mapper)
    {
        _orchestrator = orchestrator;
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<Result<GenerationWorkflowDto>> Handle(CreateGenerationWorkflowCommand request, CancellationToken cancellationToken)
    {
        var steps = request.Request.Steps.Select(s => new OrchestrationStep(
            s.Name,
            s.Type,
            s.DependsOn,
            s.ParentId,
            s.Children,
            s.ParallelGroupId,
            s.SequentialGroupId,
            s.Condition,
            s.Provider,
            s.Resolution,
            s.Style,
            s.AspectRatio,
            s.Model,
            s.TimeoutSeconds,
            s.MaxRetries,
            s.Inputs,
            s.Id));

        WorkflowPolicy? policy = null;
        if (request.Request.Policy is not null)
        {
            policy = new WorkflowPolicy(
                request.Request.Policy.MaxRetry,
                request.Request.Policy.ContinueOnFailure,
                request.Request.Policy.Parallelism,
                request.Request.Policy.BatchSize,
                request.Request.Policy.ProviderFallback,
                request.Request.Policy.TimeoutSeconds,
                request.Request.Policy.Cancellation);
        }

        WorkflowExecutionContext? context = null;
        if (request.Request.Context is not null)
        {
            context = new WorkflowExecutionContext(request.Request.Context);
        }

        var result = await _orchestrator.CreateWorkflowAsync(
            request.Request.ProjectId,
            request.OwnerId,
            request.Request.Name,
            request.Request.Description,
            steps,
            policy,
            context,
            request.Request.SceneId,
            request.Request.ShotId,
            cancellationToken);

        return result.IsSuccess
            ? Result<GenerationWorkflowDto>.Success(_mapper.Map<GenerationWorkflowDto>(result.Value!))
            : Result<GenerationWorkflowDto>.Failure(result.Error);
    }

    public async Task<Result<GenerationWorkflowDto>> Handle(QueueGenerationWorkflowCommand request, CancellationToken cancellationToken)
    {
        var result = await _orchestrator.QueueWorkflowAsync(request.WorkflowId, cancellationToken);
        return result.IsSuccess
            ? Result<GenerationWorkflowDto>.Success(_mapper.Map<GenerationWorkflowDto>(result.Value!))
            : Result<GenerationWorkflowDto>.Failure(result.Error);
    }

    public async Task<Result<WorkflowResult>> Handle(RunGenerationWorkflowCommand request, CancellationToken cancellationToken)
    {
        return await _orchestrator.ExecuteWorkflowAsync(request.WorkflowId, cancellationToken);
    }

    public async Task<Result<GenerationWorkflowDto>> Handle(RetryGenerationWorkflowCommand request, CancellationToken cancellationToken)
    {
        var result = await _orchestrator.RetryWorkflowAsync(request.WorkflowId, request.StepId, cancellationToken);
        return result.IsSuccess
            ? Result<GenerationWorkflowDto>.Success(_mapper.Map<GenerationWorkflowDto>(result.Value!))
            : Result<GenerationWorkflowDto>.Failure(result.Error);
    }

    public async Task<Result<GenerationWorkflowDto>> Handle(ResumeGenerationWorkflowCommand request, CancellationToken cancellationToken)
    {
        var result = await _orchestrator.ResumeWorkflowAsync(request.WorkflowId, cancellationToken);
        return result.IsSuccess
            ? Result<GenerationWorkflowDto>.Success(_mapper.Map<GenerationWorkflowDto>(result.Value!))
            : Result<GenerationWorkflowDto>.Failure(result.Error);
    }

    public async Task<Result> Handle(CancelGenerationWorkflowCommand request, CancellationToken cancellationToken)
    {
        return await _orchestrator.CancelWorkflowAsync(request.WorkflowId, request.Reason, cancellationToken);
    }

    public async Task<Result<GenerationWorkflowDto>> Handle(GetGenerationWorkflowQuery request, CancellationToken cancellationToken)
    {
        var result = await _orchestrator.GetWorkflowAsync(request.WorkflowId, cancellationToken);
        return result.IsSuccess
            ? Result<GenerationWorkflowDto>.Success(_mapper.Map<GenerationWorkflowDto>(result.Value!))
            : Result<GenerationWorkflowDto>.Failure(result.Error);
    }

    public async Task<Result<WorkflowState>> Handle(GetGenerationWorkflowStatusQuery request, CancellationToken cancellationToken)
    {
        return await _orchestrator.GetWorkflowStatusAsync(request.WorkflowId, cancellationToken);
    }

    public async Task<Result<List<WorkflowHistoryDto>>> Handle(GetGenerationWorkflowHistoryQuery request, CancellationToken cancellationToken)
    {
        var result = await _orchestrator.GetWorkflowHistoryAsync(request.WorkflowId, cancellationToken);
        return result.IsSuccess
            ? Result<List<WorkflowHistoryDto>>.Success(_mapper.Map<List<WorkflowHistoryDto>>(result.Value!))
            : Result<List<WorkflowHistoryDto>>.Failure(result.Error);
    }

    public async Task<Result<PagedResult<GenerationWorkflowDto>>> Handle(GetProjectGenerationWorkflowsQuery request, CancellationToken cancellationToken)
    {
        var paged = await _repository.GetByProjectAsync(request.ProjectId, request.Page, request.PageSize, cancellationToken);
        var dtos = _mapper.Map<IReadOnlyList<GenerationWorkflowDto>>(paged.Items);
        var resultPage = new PagedResult<GenerationWorkflowDto>(dtos, paged.TotalCount, paged.Page, paged.PageSize);
        return Result<PagedResult<GenerationWorkflowDto>>.Success(resultPage);
    }
}

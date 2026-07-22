using AiVideoStudio.Application.Features.Workflows.DTOs;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using AutoMapper;
using MediatR;

namespace AiVideoStudio.Application.Features.Workflows.Handlers;

public sealed class WorkflowCommandHandlers :
    IRequestHandler<CreateWorkflowCommand, Result<WorkflowDto>>,
    IRequestHandler<UpdateWorkflowCommand, Result<WorkflowDto>>,
    IRequestHandler<DeleteWorkflowCommand, Result>,
    IRequestHandler<RunWorkflowCommand, Result<WorkflowDto>>,
    IRequestHandler<CancelWorkflowCommand, Result>,
    IRequestHandler<RetryWorkflowCommand, Result<WorkflowDto>>,
    IRequestHandler<PauseWorkflowCommand, Result>,
    IRequestHandler<ResumeWorkflowCommand, Result>,
    IRequestHandler<UpdateWorkflowVariablesCommand, Result<WorkflowDto>>
{
    private readonly IAIWorkflowRepository _workflows;
    private readonly IProjectRepository _projects;
    private readonly IWorkflowScheduler _scheduler;
    private readonly IWorkflowExecutor _executor;
    private readonly ICurrentUser _user;
    private readonly IMapper _mapper;

    public WorkflowCommandHandlers(IAIWorkflowRepository workflows, IProjectRepository projects,
        IWorkflowScheduler scheduler, IWorkflowExecutor executor, ICurrentUser user, IMapper mapper)
    { _workflows=workflows;_projects=projects;_scheduler=scheduler;_executor=executor;_user=user;_mapper=mapper; }

    public async Task<Result<WorkflowDto>> Handle(CreateWorkflowCommand request,CancellationToken ct)
    {
        if(!Authenticated()) return Result<WorkflowDto>.Failure(WorkflowErrors.Unauthorized);
        var project=await _projects.GetByIdAsync(request.ProjectId,ct);
        if(project is null)return Result<WorkflowDto>.Failure(WorkflowErrors.ProjectNotFound);
        if(!Access(project.OwnerId))return Result<WorkflowDto>.Failure(WorkflowErrors.Forbidden);
        try
        {
            var workflow=AIWorkflow.Create(request.ProjectId,_user.UserId!,request.Name,request.Description,
                request.Steps.Select(ToStep),request.Variables?.Select(x=>new WorkflowVariable(x.Key,x.Value)));
            await _workflows.AddAsync(workflow,ct);
            return Result<WorkflowDto>.Success(_mapper.Map<WorkflowDto>(workflow));
        }
        catch(InvalidOperationException){return Result<WorkflowDto>.Failure(WorkflowErrors.InvalidGraph);}
    }

    public async Task<Result<WorkflowDto>> Handle(UpdateWorkflowCommand request,CancellationToken ct)
    {
        var guard=await GetOwned<WorkflowDto>(request.Id,ct);if(guard.Error is not null)return guard.Error;
        try{guard.Workflow!.Update(request.Name,request.Description,request.Steps.Select(ToStep));await _workflows.UpdateAsync(guard.Workflow,ct);return Result<WorkflowDto>.Success(_mapper.Map<WorkflowDto>(guard.Workflow));}
        catch(InvalidOperationException e){return Result<WorkflowDto>.Failure(e.Message.Contains("graph",StringComparison.OrdinalIgnoreCase)||e.Message.Contains("dependency",StringComparison.OrdinalIgnoreCase)||e.Message.Contains("step id",StringComparison.OrdinalIgnoreCase)?WorkflowErrors.InvalidGraph:WorkflowErrors.InvalidState);}
    }

    public async Task<Result> Handle(DeleteWorkflowCommand request,CancellationToken ct)
    {var guard=await GetOwned<object>(request.Id,ct);if(guard.Error is not null)return Result.Failure(guard.Error.Error);try{guard.Workflow!.SoftDelete();await _workflows.UpdateAsync(guard.Workflow,ct);return Result.Success();}catch(InvalidOperationException){return Result.Failure(WorkflowErrors.InvalidState);}}

    public async Task<Result<WorkflowDto>> Handle(RunWorkflowCommand request,CancellationToken ct)
    {var guard=await GetOwned<WorkflowDto>(request.Id,ct);if(guard.Error is not null)return guard.Error;if(guard.Workflow!.Status!=Domain.Enums.WorkflowStatus.Ready)return Result<WorkflowDto>.Failure(WorkflowErrors.InvalidState);await _scheduler.ScheduleAsync(request.Id,ct);return Result<WorkflowDto>.Success(_mapper.Map<WorkflowDto>(guard.Workflow));}

    public async Task<Result> Handle(CancelWorkflowCommand request,CancellationToken ct)
    {var guard=await GetOwned<object>(request.Id,ct);if(guard.Error is not null)return Result.Failure(guard.Error.Error);try{guard.Workflow!.Cancel();_scheduler.Remove(request.Id);_executor.Cancel(request.Id);await _workflows.UpdateAsync(guard.Workflow,ct);return Result.Success();}catch(InvalidOperationException){return Result.Failure(WorkflowErrors.InvalidState);}}

    public async Task<Result<WorkflowDto>> Handle(RetryWorkflowCommand request,CancellationToken ct)
    {var guard=await GetOwned<WorkflowDto>(request.Id,ct);if(guard.Error is not null)return guard.Error;try{guard.Workflow!.Retry();await _workflows.UpdateAsync(guard.Workflow,ct);await _scheduler.ScheduleAsync(request.Id,ct);return Result<WorkflowDto>.Success(_mapper.Map<WorkflowDto>(guard.Workflow));}catch(InvalidOperationException){return Result<WorkflowDto>.Failure(WorkflowErrors.InvalidState);}}

    public async Task<Result> Handle(PauseWorkflowCommand request,CancellationToken ct)
    {var guard=await GetOwned<object>(request.Id,ct);if(guard.Error is not null)return Result.Failure(guard.Error.Error);try{guard.Workflow!.Pause();await _workflows.UpdateAsync(guard.Workflow,ct);return Result.Success();}catch(InvalidOperationException){return Result.Failure(WorkflowErrors.InvalidState);}}

    public async Task<Result> Handle(ResumeWorkflowCommand request,CancellationToken ct)
    {var guard=await GetOwned<object>(request.Id,ct);if(guard.Error is not null)return Result.Failure(guard.Error.Error);try{guard.Workflow!.Resume();await _workflows.UpdateAsync(guard.Workflow,ct);return Result.Success();}catch(InvalidOperationException){return Result.Failure(WorkflowErrors.InvalidState);}}

    public async Task<Result<WorkflowDto>> Handle(UpdateWorkflowVariablesCommand request,CancellationToken ct)
    {var guard=await GetOwned<WorkflowDto>(request.Id,ct);if(guard.Error is not null)return guard.Error;guard.Workflow!.UpdateVariables(request.Variables.ToDictionary());await _workflows.UpdateAsync(guard.Workflow,ct);return Result<WorkflowDto>.Success(_mapper.Map<WorkflowDto>(guard.Workflow));}

    private async Task<(AIWorkflow? Workflow,Result<T>? Error)> GetOwned<T>(string id,CancellationToken ct)
    {var w=await _workflows.GetByIdAsync(id,ct);if(w is null)return(null,Result<T>.Failure(WorkflowErrors.NotFound));if(!Authenticated())return(null,Result<T>.Failure(WorkflowErrors.Unauthorized));if(!Access(w.OwnerId))return(null,Result<T>.Failure(WorkflowErrors.Forbidden));return(w,null);}
    private static WorkflowStep ToStep(WorkflowStepDefinition x)=>new(x.Name,x.Type,x.Dependencies,x.Condition,x.TimeoutSeconds,x.MaxRetries,x.InputContext?.ToDictionary(),x.Id);
    private bool Authenticated()=>_user.IsAuthenticated&&!string.IsNullOrWhiteSpace(_user.UserId);
    private bool Access(string owner)=>owner==_user.UserId||_user.Roles.Contains("Admin")||_user.Roles.Contains("Administrator");
}

public sealed class WorkflowQueryHandlers :
    IRequestHandler<GetWorkflowByIdQuery,Result<WorkflowDto>>,
    IRequestHandler<GetProjectWorkflowsQuery,Result<PagedResult<WorkflowSummaryDto>>>,
    IRequestHandler<GetWorkflowExecutionQuery,Result<WorkflowExecutionDto>>
{
    private readonly IAIWorkflowRepository _workflows;private readonly IWorkflowExecutionRepository _executions;private readonly IProjectRepository _projects;private readonly ICurrentUser _user;private readonly IMapper _mapper;
    public WorkflowQueryHandlers(IAIWorkflowRepository workflows,IWorkflowExecutionRepository executions,IProjectRepository projects,ICurrentUser user,IMapper mapper){_workflows=workflows;_executions=executions;_projects=projects;_user=user;_mapper=mapper;}
    public async Task<Result<WorkflowDto>> Handle(GetWorkflowByIdQuery q,CancellationToken ct){var w=await _workflows.GetByIdAsync(q.Id,ct);var e=Guard<WorkflowDto>(w?.OwnerId,w is null);return e??Result<WorkflowDto>.Success(_mapper.Map<WorkflowDto>(w));}
    public async Task<Result<PagedResult<WorkflowSummaryDto>>> Handle(GetProjectWorkflowsQuery q,CancellationToken ct){if(!_user.IsAuthenticated)return Result<PagedResult<WorkflowSummaryDto>>.Failure(WorkflowErrors.Unauthorized);var p=await _projects.GetByIdAsync(q.ProjectId,ct);if(p is null)return Result<PagedResult<WorkflowSummaryDto>>.Failure(WorkflowErrors.ProjectNotFound);if(!Access(p.OwnerId))return Result<PagedResult<WorkflowSummaryDto>>.Failure(WorkflowErrors.Forbidden);var page=await _workflows.GetByProjectAsync(q.ProjectId,q.Page,q.PageSize,ct);return Result<PagedResult<WorkflowSummaryDto>>.Success(new(page.Items.Select(_mapper.Map<WorkflowSummaryDto>).ToList(),page.TotalCount,page.Page,page.PageSize));}
    public async Task<Result<WorkflowExecutionDto>> Handle(GetWorkflowExecutionQuery q,CancellationToken ct){var w=await _workflows.GetByIdAsync(q.WorkflowId,ct);var guard=Guard<WorkflowExecutionDto>(w?.OwnerId,w is null);if(guard is not null)return guard;var execution=await _executions.GetLatestAsync(q.WorkflowId,ct);return execution is null?Result<WorkflowExecutionDto>.Failure(WorkflowErrors.ExecutionNotFound):Result<WorkflowExecutionDto>.Success(_mapper.Map<WorkflowExecutionDto>(execution));}
    private Result<T>? Guard<T>(string? owner,bool missing){if(missing)return Result<T>.Failure(WorkflowErrors.NotFound);if(!_user.IsAuthenticated)return Result<T>.Failure(WorkflowErrors.Unauthorized);if(!Access(owner!))return Result<T>.Failure(WorkflowErrors.Forbidden);return null;}
    private bool Access(string owner)=>owner==_user.UserId||_user.Roles.Contains("Admin")||_user.Roles.Contains("Administrator");
}

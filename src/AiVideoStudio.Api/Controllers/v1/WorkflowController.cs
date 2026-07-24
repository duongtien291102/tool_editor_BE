using AiVideoStudio.Application.Features.Workflows;
using AiVideoStudio.Application.Features.Workflows.DTOs;
using AiVideoStudio.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiVideoStudio.Api.Controllers.v1;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class WorkflowController:ControllerBase
{
    private readonly IMediator _mediator;public WorkflowController(IMediator mediator)=>_mediator=mediator;

    /// <summary>Create a validated DAG workflow for a project.</summary>
    [HttpPost("workflows")][ProducesResponseType(typeof(ApiResponse<WorkflowDto>),201)]
    public async Task<IActionResult> Create(CreateWorkflowRequest r,CancellationToken ct){var x=await _mediator.Send(new CreateWorkflowCommand(r.ProjectId,r.Name,r.Description,r.Steps,r.Variables),ct);return x.IsSuccess?CreatedAtAction(nameof(Get),new{id=x.Value!.Id},ApiResponse<WorkflowDto>.SuccessResponse(x.Value)):Failure(x);}

    /// <summary>Get a workflow and all step states.</summary>
    [HttpGet("workflows/{id}")][ProducesResponseType(typeof(ApiResponse<WorkflowDto>),200)]
    public async Task<IActionResult> Get(string id,CancellationToken ct){var x=await _mediator.Send(new GetWorkflowByIdQuery(id),ct);return x.IsSuccess?Ok(ApiResponse<WorkflowDto>.SuccessResponse(x.Value!)):Failure(x);}

    /// <summary>List project workflows using stable pagination.</summary>
    [HttpGet("projects/{projectId}/workflows")][ProducesResponseType(typeof(ApiResponse<PagedResult<WorkflowSummaryDto>>),200)]
    public async Task<IActionResult> List(string projectId,[FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken ct=default){var x=await _mediator.Send(new GetProjectWorkflowsQuery(projectId,page,pageSize),ct);return x.IsSuccess?Ok(ApiResponse<PagedResult<WorkflowSummaryDto>>.SuccessResponse(x.Value!)):Failure(x);}

    /// <summary>Queue a ready workflow for background execution.</summary>
    [HttpPost("workflows/{id}/run")][ProducesResponseType(typeof(ApiResponse<WorkflowDto>),202)]
    public async Task<IActionResult> Run(string id,CancellationToken ct){var x=await _mediator.Send(new RunWorkflowCommand(id),ct);return x.IsSuccess?Accepted(ApiResponse<WorkflowDto>.SuccessResponse(x.Value!,"Workflow queued.")):Failure(x);}

    /// <summary>Cancel queued or active execution.</summary>
    [HttpPost("workflows/{id}/cancel")][ProducesResponseType(typeof(ApiResponse<object>),200)]
    public Task<IActionResult> Cancel(string id,CancellationToken ct)=>Send(new CancelWorkflowCommand(id),"Workflow cancelled.",ct);

    /// <summary>Reset and queue a failed workflow.</summary>
    [HttpPost("workflows/{id}/retry")][ProducesResponseType(typeof(ApiResponse<WorkflowDto>),202)]
    public async Task<IActionResult> Retry(string id,CancellationToken ct){var x=await _mediator.Send(new RetryWorkflowCommand(id),ct);return x.IsSuccess?Accepted(ApiResponse<WorkflowDto>.SuccessResponse(x.Value!,"Workflow queued for retry.")):Failure(x);}

    /// <summary>Pause an active workflow at a step boundary.</summary>
    [HttpPost("workflows/{id}/pause")][ProducesResponseType(typeof(ApiResponse<object>),200)]
    public Task<IActionResult> Pause(string id,CancellationToken ct)=>Send(new PauseWorkflowCommand(id),"Workflow paused.",ct);

    /// <summary>Resume a paused workflow.</summary>
    [HttpPost("workflows/{id}/resume")][ProducesResponseType(typeof(ApiResponse<object>),200)]
    public Task<IActionResult> Resume(string id,CancellationToken ct)=>Send(new ResumeWorkflowCommand(id),"Workflow resumed.",ct);

    /// <summary>Replace workflow metadata and DAG while it is not running.</summary>
    [HttpPut("workflows/{id}")][ProducesResponseType(typeof(ApiResponse<WorkflowDto>),200)]
    public async Task<IActionResult> Update(string id,UpdateWorkflowRequest r,CancellationToken ct){var x=await _mediator.Send(new UpdateWorkflowCommand(id,r.Name,r.Description,r.Steps),ct);return x.IsSuccess?Ok(ApiResponse<WorkflowDto>.SuccessResponse(x.Value!)):Failure(x);}

    /// <summary>Soft-delete a workflow that is not running.</summary>
    [HttpDelete("workflows/{id}")][ProducesResponseType(204)]
    public async Task<IActionResult> Delete(string id,CancellationToken ct){var x=await _mediator.Send(new DeleteWorkflowCommand(id),ct);return x.IsSuccess?NoContent():Failure(x);}

    /// <summary>Get the most recent workflow execution.</summary>
    [HttpGet("workflows/{id}/execution")][ProducesResponseType(typeof(ApiResponse<WorkflowExecutionDto>),200)]
    public async Task<IActionResult> Execution(string id,CancellationToken ct){var x=await _mediator.Send(new GetWorkflowExecutionQuery(id),ct);return x.IsSuccess?Ok(ApiResponse<WorkflowExecutionDto>.SuccessResponse(x.Value!)):Failure(x);}

    /// <summary>Get workflow status.</summary>
    [HttpGet("workflows/{id}/status")][ProducesResponseType(typeof(ApiResponse<object>),200)]
    public async Task<IActionResult> Status(string id,CancellationToken ct){var x=await _mediator.Send(new AiVideoStudio.Application.Features.Orchestration.Commands.GetGenerationWorkflowStatusQuery(id),ct);if(x.IsSuccess)return Ok(ApiResponse<object>.SuccessResponse(x.Value!));var legacy=await _mediator.Send(new GetWorkflowByIdQuery(id),ct);return legacy.IsSuccess?Ok(ApiResponse<object>.SuccessResponse(legacy.Value!.Status)):Failure(x);}

    /// <summary>Get workflow execution history.</summary>
    [HttpGet("workflows/{id}/history")][ProducesResponseType(typeof(ApiResponse<object>),200)]
    public async Task<IActionResult> History(string id,CancellationToken ct){var x=await _mediator.Send(new AiVideoStudio.Application.Features.Orchestration.Commands.GetGenerationWorkflowHistoryQuery(id),ct);return x.IsSuccess?Ok(ApiResponse<object>.SuccessResponse(x.Value!)):Failure(x);}


    private async Task<IActionResult> Send(IRequest<Result> command,string message,CancellationToken ct){var x=await _mediator.Send(command,ct);return x.IsSuccess?Ok(ApiResponse<object>.SuccessResponse(null!,message)):Failure(x);}
    private IActionResult Failure(Result x){var body=ApiResponse<object>.FailureResponse(x.Error.Message,x.ValidationErrors,x.Error.Code);if(x.Error.Code.Contains("Unauthorized",StringComparison.Ordinal))return Unauthorized(body);if(x.Error.Code.Contains("Forbidden",StringComparison.Ordinal))return StatusCode(403,body);if(x.Error.Code.Contains("NotFound",StringComparison.Ordinal))return NotFound(body);return BadRequest(body);}
}

public sealed class CreateWorkflowRequest{public string ProjectId{get;set;}=string.Empty;public string Name{get;set;}=string.Empty;public string? Description{get;set;}public List<WorkflowStepDefinition> Steps{get;set;}=new();public Dictionary<string,string>? Variables{get;set;}}
public sealed class UpdateWorkflowRequest{public string Name{get;set;}=string.Empty;public string? Description{get;set;}public List<WorkflowStepDefinition> Steps{get;set;}=new();}

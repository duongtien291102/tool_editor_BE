using System.Security.Claims;
using AiVideoStudio.Application.Features.Orchestration.Commands;
using AiVideoStudio.Application.Features.Orchestration.DTOs;
using AiVideoStudio.Domain.Entities.Orchestration;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiVideoStudio.Api.Controllers.v1;

[ApiController]
[Authorize]
[Route("api/v1/generation-workflows")]
public sealed class GenerationWorkflowController : ControllerBase
{
    private readonly IMediator _mediator;

    public GenerationWorkflowController(IMediator mediator) => _mediator = mediator;

    /// <summary>Create a new AI Generation Orchestration Workflow.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<GenerationWorkflowDto>), 201)]
    public async Task<IActionResult> Create([FromBody] CreateGenerationWorkflowRequest request, CancellationToken ct)
    {
        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var result = await _mediator.Send(new CreateGenerationWorkflowCommand(request, ownerId), ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, ApiResponse<GenerationWorkflowDto>.SuccessResponse(result.Value))
            : Failure(result);
    }

    /// <summary>Get an AI Generation Workflow by Id.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<GenerationWorkflowDto>), 200)]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetGenerationWorkflowQuery(id), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<GenerationWorkflowDto>.SuccessResponse(result.Value!))
            : Failure(result);
    }

    /// <summary>Get status of an AI Generation Workflow.</summary>
    [HttpGet("{id}/status")]
    [ProducesResponseType(typeof(ApiResponse<WorkflowState>), 200)]
    public async Task<IActionResult> GetStatus(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetGenerationWorkflowStatusQuery(id), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<WorkflowState>.SuccessResponse(result.Value))
            : Failure(result);
    }

    /// <summary>Get execution history logs of an AI Generation Workflow.</summary>
    [HttpGet("{id}/history")]
    [ProducesResponseType(typeof(ApiResponse<List<WorkflowHistoryDto>>), 200)]
    public async Task<IActionResult> GetHistory(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetGenerationWorkflowHistoryQuery(id), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<List<WorkflowHistoryDto>>.SuccessResponse(result.Value!))
            : Failure(result);
    }

    /// <summary>Retry a failed AI Generation Workflow.</summary>
    [HttpPost("{id}/retry")]
    [ProducesResponseType(typeof(ApiResponse<GenerationWorkflowDto>), 202)]
    public async Task<IActionResult> Retry(string id, [FromQuery] string? stepId, CancellationToken ct)
    {
        var result = await _mediator.Send(new RetryGenerationWorkflowCommand(id, stepId), ct);
        return result.IsSuccess
            ? Accepted(ApiResponse<GenerationWorkflowDto>.SuccessResponse(result.Value!, "Workflow queued for retry."))
            : Failure(result);
    }

    /// <summary>Resume a paused or waiting AI Generation Workflow.</summary>
    [HttpPost("{id}/resume")]
    [ProducesResponseType(typeof(ApiResponse<GenerationWorkflowDto>), 202)]
    public async Task<IActionResult> Resume(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ResumeGenerationWorkflowCommand(id), ct);
        return result.IsSuccess
            ? Accepted(ApiResponse<GenerationWorkflowDto>.SuccessResponse(result.Value!, "Workflow resumed."))
            : Failure(result);
    }

    /// <summary>Cancel an active or queued AI Generation Workflow.</summary>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> Cancel(string id, [FromQuery] string reason = "User requested cancellation.", CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CancelGenerationWorkflowCommand(id, reason), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.SuccessResponse(null!, "Workflow cancelled successfully."))
            : Failure(result);
    }

    private IActionResult Failure<T>(Result<T> x)
    {
        var body = ApiResponse<object>.FailureResponse(x.Error.Message, x.ValidationErrors, x.Error.Code);
        if (x.Error.Code.Contains("Unauthorized", StringComparison.Ordinal)) return Unauthorized(body);
        if (x.Error.Code.Contains("Forbidden", StringComparison.Ordinal)) return StatusCode(403, body);
        if (x.Error.Code.Contains("NotFound", StringComparison.Ordinal)) return NotFound(body);
        return BadRequest(body);
    }

    private IActionResult Failure(Result x)
    {
        var body = ApiResponse<object>.FailureResponse(x.Error.Message, x.ValidationErrors, x.Error.Code);
        if (x.Error.Code.Contains("Unauthorized", StringComparison.Ordinal)) return Unauthorized(body);
        if (x.Error.Code.Contains("Forbidden", StringComparison.Ordinal)) return StatusCode(403, body);
        if (x.Error.Code.Contains("NotFound", StringComparison.Ordinal)) return NotFound(body);
        return BadRequest(body);
    }
}

using System.Text.Json;
using System.Threading.Tasks;
using AiVideoStudio.Application.Features.RenderJobs;
using AiVideoStudio.Application.Features.RenderJobs.DTOs;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AiVideoStudio.Api.Controllers.v1;

[ApiController]
[Route("api/v1")]
[Authorize]
public class RenderJobsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RenderJobsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Queue a new render job.
    /// </summary>
    [HttpPost("render-jobs")]
    [ProducesResponseType(typeof(ApiResponse<RenderJobDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateJob([FromBody] CreateRenderJobRequest request)
    {
        var command = new CreateRenderJobCommand(
            request.ProjectId,
            request.JobType,
            request.Provider,
            request.Priority,
            request.MaxRetryCount,
            request.TimelineId,
            request.ScriptId,
            request.InputPayload);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.Error.Code.Contains("Unauthorized"))
                return Forbid();

            return BadRequest(ApiResponse<object>.FailureResponse(result.Error.Message, result.ValidationErrors, result.Error.Code));
        }

        return CreatedAtAction(
            nameof(GetJobById),
            new { id = result.Value!.Id },
            ApiResponse<RenderJobDto>.SuccessResponse(result.Value, "Render job queued successfully."));
    }

    /// <summary>
    /// Get a render job by ID.
    /// </summary>
    [HttpGet("render-jobs/{id}")]
    [ProducesResponseType(typeof(ApiResponse<RenderJobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobById(string id)
    {
        var query = new GetRenderJobByIdQuery(id);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            if (result.Error.Code.Contains("NotFound"))
                return NotFound(ApiResponse<object>.FailureResponse(result.Error.Message, result.ValidationErrors, result.Error.Code));
            if (result.Error.Code.Contains("Unauthorized"))
                return Forbid();

            return BadRequest(ApiResponse<object>.FailureResponse(result.Error.Message, result.ValidationErrors, result.Error.Code));
        }

        return Ok(ApiResponse<RenderJobDto>.SuccessResponse(result.Value!));
    }

    /// <summary>
    /// Get list of render jobs (globally, filtered).
    /// </summary>
    [HttpGet("render-jobs")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<RenderJobSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRenderJobs(
        [FromQuery] string? projectId,
        [FromQuery] string? status,
        [FromQuery] string? provider,
        [FromQuery] string? priority,
        [FromQuery] string? search,
        [FromQuery] string? sortBy,
        [FromQuery] bool descending = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetRenderJobsQuery(
            projectId,
            status,
            provider,
            priority,
            search,
            sortBy,
            descending,
            page,
            pageSize);

        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(result.Error.Message, result.ValidationErrors, result.Error.Code));
        }

        return Ok(ApiResponse<PagedResult<RenderJobSummaryDto>>.SuccessResponse(result.Value!));
    }

    /// <summary>
    /// Get list of render jobs for a specific project.
    /// </summary>
    [HttpGet("projects/{projectId}/render-jobs")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<RenderJobSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectRenderJobs(
        string projectId,
        [FromQuery] string? status,
        [FromQuery] string? sortBy,
        [FromQuery] bool descending = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetProjectRenderJobsQuery(
            projectId,
            status,
            sortBy,
            descending,
            page,
            pageSize);

        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            if (result.Error.Code.Contains("ProjectNotFound"))
                return NotFound(ApiResponse<object>.FailureResponse(result.Error.Message, result.ValidationErrors, result.Error.Code));
            if (result.Error.Code.Contains("Unauthorized"))
                return Forbid();

            return BadRequest(ApiResponse<object>.FailureResponse(result.Error.Message, result.ValidationErrors, result.Error.Code));
        }

        return Ok(ApiResponse<PagedResult<RenderJobSummaryDto>>.SuccessResponse(result.Value!));
    }

    /// <summary>
    /// Cancel a render job.
    /// </summary>
    [HttpPost("render-jobs/{id}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelJob(string id)
    {
        var command = new CancelRenderJobCommand(id);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.Error.Code.Contains("NotFound"))
                return NotFound(ApiResponse<object>.FailureResponse(result.Error.Message, result.ValidationErrors, result.Error.Code));
            if (result.Error.Code.Contains("Unauthorized"))
                return Forbid();

            return BadRequest(ApiResponse<object>.FailureResponse(result.Error.Message, result.ValidationErrors, result.Error.Code));
        }

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Render job cancelled successfully."));
    }

    /// <summary>
    /// Retry a failed render job.
    /// </summary>
    [HttpPost("render-jobs/{id}/retry")]
    [ProducesResponseType(typeof(ApiResponse<RenderJobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RetryJob(string id)
    {
        var command = new RetryRenderJobCommand(id);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.Error.Code.Contains("NotFound"))
                return NotFound(ApiResponse<object>.FailureResponse(result.Error.Message, result.ValidationErrors, result.Error.Code));
            if (result.Error.Code.Contains("Unauthorized"))
                return Forbid();

            return BadRequest(ApiResponse<object>.FailureResponse(result.Error.Message, result.ValidationErrors, result.Error.Code));
        }

        return Ok(ApiResponse<RenderJobDto>.SuccessResponse(result.Value!, "Render job queued for retry."));
    }
}

// DTO for Create Request (omitting System.Text.Json elements from directly binding the controller if needed, but JsonDocument works fine in .NET Core API if sent properly)
public class CreateRenderJobRequest
{
    public string ProjectId { get; set; } = string.Empty;
    public RenderJobType JobType { get; set; }
    public RenderProvider Provider { get; set; }
    public RenderPriority Priority { get; set; } = RenderPriority.Normal;
    public int MaxRetryCount { get; set; } = 3;
    public string? TimelineId { get; set; }
    public string? ScriptId { get; set; }
    public JsonDocument? InputPayload { get; set; }
}

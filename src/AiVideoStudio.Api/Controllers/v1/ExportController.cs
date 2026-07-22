using AiVideoStudio.Application.Features.Exports;
using AiVideoStudio.Application.Features.Exports.DTOs;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiVideoStudio.Api.Controllers.v1;

[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class ExportController : ControllerBase
{
    private readonly IMediator _mediator;
    public ExportController(IMediator mediator) => _mediator = mediator;

    /// <summary>Create and queue a timeline export.</summary>
    [HttpPost("export")]
    [ProducesResponseType(typeof(ApiResponse<ExportJobDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateExportRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateExportJobCommand(
            request.RenderJobId,
            request.ProjectId,
            request.TimelineId,
            request.VideoCodec,
            request.AudioCodec,
            request.Container,
            request.MaxRetryCount), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result);
        return CreatedAtAction(nameof(Get), new { id = result.Value!.Id },
            ApiResponse<ExportJobDto>.SuccessResponse(result.Value, "Export queued successfully."));
    }

    /// <summary>Get an export job by ID.</summary>
    [HttpGet("export/{id}")]
    [ProducesResponseType(typeof(ApiResponse<ExportJobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetExportJobQuery(id), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result);
        return Ok(ApiResponse<ExportJobDto>.SuccessResponse(result.Value!));
    }

    /// <summary>List export jobs for a project.</summary>
    [HttpGet("projects/{projectId}/exports")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ExportSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        string projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetProjectExportJobsQuery(projectId, page, pageSize), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result);
        return Ok(ApiResponse<PagedResult<ExportSummaryDto>>.SuccessResponse(result.Value!));
    }

    /// <summary>Cancel a pending or active export.</summary>
    [HttpPost("export/{id}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Cancel(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelExportJobCommand(id), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result);
        return Ok(ApiResponse<object>.SuccessResponse(null!, "Export cancelled successfully."));
    }

    /// <summary>Retry a failed export.</summary>
    [HttpPost("export/{id}/retry")]
    [ProducesResponseType(typeof(ApiResponse<ExportJobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Retry(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RetryExportJobCommand(id), cancellationToken);
        if (!result.IsSuccess) return MapFailure(result);
        return Ok(ApiResponse<ExportJobDto>.SuccessResponse(result.Value!, "Export queued for retry."));
    }

    private IActionResult MapFailure(Result result)
    {
        var response = ApiResponse<object>.FailureResponse(
            result.Error.Message,
            result.ValidationErrors,
            result.Error.Code);
        if (result.Error.Code.Contains("Unauthorized", StringComparison.Ordinal)) return Unauthorized(response);
        if (result.Error.Code.Contains("Forbidden", StringComparison.Ordinal)) return StatusCode(StatusCodes.Status403Forbidden, response);
        if (result.Error.Code.Contains("NotFound", StringComparison.Ordinal)) return NotFound(response);
        return BadRequest(response);
    }
}

public sealed class CreateExportRequest
{
    public string RenderJobId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string TimelineId { get; set; } = string.Empty;
    public VideoCodec VideoCodec { get; set; } = VideoCodec.H264;
    public AudioCodec AudioCodec { get; set; } = AudioCodec.AAC;
    public ContainerFormat Container { get; set; } = ContainerFormat.MP4;
    public int MaxRetryCount { get; set; } = 3;
}

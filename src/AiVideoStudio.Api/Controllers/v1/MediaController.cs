using AiVideoStudio.Application.Features.Media.Commands;
using AiVideoStudio.Application.Features.Media.DTOs;
using AiVideoStudio.Application.Features.Media.Queries;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.ApiContracts.V1.Media.Requests;
using AiVideoStudio.Shared.Responses;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Api.Controllers.v1;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
public class MediaController : ControllerBase
{
    private readonly ISender _mediator;

    public MediaController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Uploads a media asset for a project using multipart/form-data.
    /// </summary>
    [HttpPost("media/upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MediaDto>>> Upload([FromForm] IFormFile file, [FromForm] string projectId, CancellationToken cancellationToken)
    {
        if (file == null || file.Length <= 0)
        {
            return BadRequest(ApiResponse<MediaDto>.FailureResponse("File is required and cannot be empty.", new[] { "File is required and cannot be empty." }, "INVALID_FILE"));
        }

        if (string.IsNullOrWhiteSpace(projectId))
        {
            return BadRequest(ApiResponse<MediaDto>.FailureResponse("ProjectId is required.", new[] { "ProjectId is required." }, "INVALID_PROJECT_ID"));
        }

        using var stream = file.OpenReadStream();
        var command = new UploadMediaCommand(
            projectId,
            file.FileName,
            file.ContentType,
            file.Length,
            stream
        );

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            return Ok(ApiResponse<MediaDto>.SuccessResponse(result.Value));
        }

        return HandleFailure<MediaDto>(result);
    }

    /// <summary>
    /// Gets a media asset by ID.
    /// </summary>
    [HttpGet("media/{id}")]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MediaDto>>> GetById([FromRoute] string id, CancellationToken cancellationToken)
    {
        var query = new GetMediaByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            return Ok(ApiResponse<MediaDto>.SuccessResponse(result.Value));
        }

        return HandleFailure<MediaDto>(result);
    }

    /// <summary>
    /// Gets a paginated list of media assets for a project.
    /// </summary>
    [HttpGet("projects/{projectId}/media")]
    [ProducesResponseType(typeof(ApiResponse<MediaListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MediaListResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<MediaListResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<MediaListResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MediaListResponse>>> GetProjectMedia(
        [FromRoute] string projectId,
        [FromQuery] GetProjectMediaQueryRequest request,
        CancellationToken cancellationToken)
    {
        AssetType? assetType = !string.IsNullOrEmpty(request.AssetType) && Enum.TryParse<AssetType>(request.AssetType, true, out var at)
            ? at
            : null;

        MediaStatus? status = !string.IsNullOrEmpty(request.Status) && Enum.TryParse<MediaStatus>(request.Status, true, out var ms)
            ? ms
            : null;

        var query = new GetProjectMediaQuery(
            projectId,
            request.Page,
            request.PageSize,
            request.Search,
            request.SortBy,
            request.SortDescending,
            assetType,
            status
        );

        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            return Ok(ApiResponse<MediaListResponse>.SuccessResponse(result.Value));
        }

        return HandleFailure<MediaListResponse>(result);
    }

    /// <summary>
    /// Updates media asset metadata.
    /// </summary>
    [HttpPut("media/{id}")]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MediaDto>>> Update([FromRoute] string id, [FromBody] UpdateMediaRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateMediaCommand(
            id,
            request.FileName,
            request.Width,
            request.Height,
            request.Duration,
            request.ThumbnailPath
        );

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            return Ok(ApiResponse<MediaDto>.SuccessResponse(result.Value));
        }

        return HandleFailure<MediaDto>(result);
    }

    /// <summary>
    /// Soft deletes a media asset by ID.
    /// </summary>
    [HttpDelete("media/{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete([FromRoute] string id, CancellationToken cancellationToken)
    {
        var command = new DeleteMediaCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(ApiResponse<bool>.SuccessResponse(true));
        }

        return HandleFailure<bool>(result);
    }

    private ActionResult<ApiResponse<T>> HandleFailure<T>(Result result)
    {
        var code = result.Error?.Code;
        var message = result.Error?.Message ?? "An error occurred.";
        var errors = result.ValidationErrors != null && result.ValidationErrors.Any()
            ? result.ValidationErrors
            : new[] { message };

        if (code == "AUTH.UNAUTHORIZED")
        {
            return Unauthorized(ApiResponse<T>.FailureResponse(message, errors, code));
        }

        if (code == "MEDIA.NOT_FOUND" || code == "MEDIA.PROJECT_NOT_FOUND")
        {
            return NotFound(ApiResponse<T>.FailureResponse(message, errors, code));
        }

        if (code == "MEDIA.UNAUTHORIZED_ACCESS")
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<T>.FailureResponse(message, errors, code));
        }

        return BadRequest(ApiResponse<T>.FailureResponse(message, errors, code));
    }
}

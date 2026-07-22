using AiVideoStudio.Application.Features.Projects.Commands;
using AiVideoStudio.Application.Features.Projects.DTOs;
using AiVideoStudio.Application.Features.Projects.Queries;
using AiVideoStudio.Shared.ApiContracts.V1.Projects.Requests;
using AiVideoStudio.Shared.Responses;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Api.Controllers.v1;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/projects")]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProjectsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new project.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> Create([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateProjectCommand(
            request.Name,
            request.Description,
            request.Thumbnail
        );

        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsSuccess && result.Value != null)
        {
            return Ok(ApiResponse<ProjectDto>.SuccessResponse(result.Value));
        }

        return HandleFailure<ProjectDto>(result);
    }

    /// <summary>
    /// Gets a project by its unique identifier.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> GetById([FromRoute] string id, CancellationToken cancellationToken)
    {
        var query = new GetProjectByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            return Ok(ApiResponse<ProjectDto>.SuccessResponse(result.Value));
        }

        return HandleFailure<ProjectDto>(result);
    }

    /// <summary>
    /// Gets a paginated list of projects with optional searching, sorting, and filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ProjectListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProjectListResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ProjectListResponse>>> GetProjects([FromQuery] GetProjectsRequest request, CancellationToken cancellationToken)
    {
        Domain.Enums.ProjectStatus? status = !string.IsNullOrEmpty(request.Status) && System.Enum.TryParse<Domain.Enums.ProjectStatus>(request.Status, true, out var s)
            ? s
            : null;

        var query = new GetProjectsQuery(
            request.Page,
            request.PageSize,
            request.Search,
            request.SortBy,
            request.SortDescending,
            status
        );

        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            return Ok(ApiResponse<ProjectListResponse>.SuccessResponse(result.Value));
        }

        return HandleFailure<ProjectListResponse>(result);
    }

    /// <summary>
    /// Updates an existing project. Only Owner or Admin can update.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> Update([FromRoute] string id, [FromBody] UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        Domain.Enums.ProjectStatus? status = !string.IsNullOrEmpty(request.Status) && System.Enum.TryParse<Domain.Enums.ProjectStatus>(request.Status, true, out var s)
            ? s
            : null;

        var command = new UpdateProjectCommand(
            id,
            request.Name,
            request.Description,
            request.Thumbnail,
            status
        );


        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            return Ok(ApiResponse<ProjectDto>.SuccessResponse(result.Value));
        }

        return HandleFailure<ProjectDto>(result);
    }

    /// <summary>
    /// Soft-deletes a project by its unique identifier. Only Owner or Admin can delete.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete([FromRoute] string id, CancellationToken cancellationToken)
    {
        var command = new DeleteProjectCommand(id);
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
        var errors = result.ValidationErrors != null && System.Linq.Enumerable.Any(result.ValidationErrors)
            ? result.ValidationErrors
            : new[] { message };

        if (code == "AUTH.UNAUTHORIZED")
        {
            return Unauthorized(ApiResponse<T>.FailureResponse(message, errors, code));
        }

        if (code == "PROJECT.NOT_FOUND")
        {
            return NotFound(ApiResponse<T>.FailureResponse(message, errors, code));
        }

        if (code == "PROJECT.UNAUTHORIZED_ACCESS")
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<T>.FailureResponse(message, errors, code));
        }

        return BadRequest(ApiResponse<T>.FailureResponse(message, errors, code));
    }
}

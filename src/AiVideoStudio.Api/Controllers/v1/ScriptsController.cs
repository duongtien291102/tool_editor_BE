using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Application.Features.Scripts.Commands;
using AiVideoStudio.Application.Features.Scripts.DTOs;
using AiVideoStudio.Application.Features.Scripts.Queries;
using AiVideoStudio.Shared.ApiContracts.V1.Scripts.Requests;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AiVideoStudio.Api.Controllers.v1;

[ApiController]
[Route("api/v1")]
[Authorize]
public class ScriptsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ScriptsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("scripts")]
    [ProducesResponseType(typeof(ApiResponse<ScriptDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateScript([FromBody] CreateScriptRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateScriptCommand(request.ProjectId, request.Name, request.Description);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Created($"/api/v1/scripts/{result.Value.Id}", ApiResponse<ScriptDto>.SuccessResponse(result.Value));
    }

    [HttpGet("scripts/{id}")]
    [ProducesResponseType(typeof(ApiResponse<ScriptDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetScriptById(string id, CancellationToken cancellationToken)
    {
        var query = new GetScriptByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Ok(ApiResponse<ScriptDto>.SuccessResponse(result.Value));
    }

    [HttpGet("projects/{projectId}/scripts")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ScriptSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetScriptsByProject(string projectId, [FromQuery] GetScriptsByProjectQueryRequest request, CancellationToken cancellationToken)
    {
        var query = new GetScriptsByProjectQuery(
            projectId,
            request.SearchTerm,
            request.IncludeDeleted,
            request.SortBy,
            request.Descending,
            request.PageNumber,
            request.PageSize);
            
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Ok(ApiResponse<PagedResult<ScriptSummaryDto>>.SuccessResponse(result.Value));
    }

    [HttpPut("scripts/{id}")]
    [ProducesResponseType(typeof(ApiResponse<ScriptDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateScript(string id, [FromBody] UpdateScriptRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateScriptCommand(id, request.Name, request.Description, request.ExpectedVersion);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Ok(ApiResponse<ScriptDto>.SuccessResponse(result.Value));
    }

    [HttpDelete("scripts/{id}")]
    [ProducesResponseType(typeof(ApiResponse<Unit>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteScript(string id, CancellationToken cancellationToken)
    {
        var command = new DeleteScriptCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Ok(ApiResponse<Unit>.SuccessResponse(Unit.Value));
    }

    [HttpPost("scripts/{id}/autosave")]
    [ProducesResponseType(typeof(ApiResponse<ScriptDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AutoSaveScript(string id, [FromBody] AutoSaveScriptRequest request, CancellationToken cancellationToken)
    {
        var command = new AutoSaveScriptCommand(id, request.Name, request.Description, request.ExpectedVersion);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Ok(ApiResponse<ScriptDto>.SuccessResponse(result.Value));
    }

    [HttpPost("scripts/{id}/scenes")]
    [ProducesResponseType(typeof(ApiResponse<SceneDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddScene(string id, [FromBody] AddSceneRequest request, CancellationToken cancellationToken)
    {
        var command = new AddSceneCommand(id, request.Name, request.Duration, request.Notes, request.ExpectedVersion);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Created($"/api/v1/scripts/{id}/scenes/{result.Value.Id}", ApiResponse<SceneDto>.SuccessResponse(result.Value));
    }

    [HttpPut("scripts/{id}/scenes/{sceneId}")]
    [ProducesResponseType(typeof(ApiResponse<SceneDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateScene(string id, string sceneId, [FromBody] UpdateSceneRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateSceneCommand(id, sceneId, request.Name, request.Duration, request.Notes, request.ExpectedVersion);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Ok(ApiResponse<SceneDto>.SuccessResponse(result.Value));
    }

    [HttpDelete("scripts/{id}/scenes/{sceneId}")]
    [ProducesResponseType(typeof(ApiResponse<Unit>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteScene(string id, string sceneId, [FromQuery] int expectedVersion, CancellationToken cancellationToken)
    {
        var command = new DeleteSceneCommand(id, sceneId, expectedVersion);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Ok(ApiResponse<Unit>.SuccessResponse(Unit.Value));
    }

    [HttpPut("scripts/{id}/scenes/reorder")]
    [ProducesResponseType(typeof(ApiResponse<Unit>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReorderScene(string id, [FromBody] ReorderSceneRequest request, CancellationToken cancellationToken)
    {
        var command = new ReorderSceneCommand(id, request.SceneId, request.NewOrder, request.ExpectedVersion);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Ok(ApiResponse<Unit>.SuccessResponse(Unit.Value));
    }
    
    [HttpPost("scripts/{id}/scenes/{sceneId}/elements")]
    [ProducesResponseType(typeof(ApiResponse<SceneElementDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddSceneElement(string id, string sceneId, [FromBody] AddSceneElementCommand request, CancellationToken cancellationToken)
    {
        // For simplicity, reusing the command directly if it matches the shape. Or could map a request.
        var command = request with { ScriptId = id, SceneId = sceneId };
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Created($"/api/v1/scripts/{id}/scenes/{sceneId}/elements/{result.Value.Id}", ApiResponse<SceneElementDto>.SuccessResponse(result.Value));
    }

    [HttpPut("scripts/{id}/scenes/{sceneId}/elements/{elementId}")]
    [ProducesResponseType(typeof(ApiResponse<SceneElementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSceneElement(string id, string sceneId, string elementId, [FromBody] UpdateSceneElementRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateSceneElementCommand(id, sceneId, elementId, request.Content, request.Metadata, request.ExpectedVersion);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Ok(ApiResponse<SceneElementDto>.SuccessResponse(result.Value));
    }
    
    [HttpDelete("scripts/{id}/scenes/{sceneId}/elements/{elementId}")]
    [ProducesResponseType(typeof(ApiResponse<Unit>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteSceneElement(string id, string sceneId, string elementId, [FromQuery] int expectedVersion, CancellationToken cancellationToken)
    {
        var command = new DeleteSceneElementCommand(id, sceneId, elementId, expectedVersion);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Ok(ApiResponse<Unit>.SuccessResponse(Unit.Value));
    }

    private IActionResult CreateErrorResponse(Error error)
    {
        if (error == ScriptErrors.NotFound || error.Code == "Project.NotFound")
            return NotFound(ApiResponse<object>.FailureResponse("Not found", new[] { error.Message }));
            
        if (error == ScriptErrors.Unauthorized)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized", new[] { error.Message }));

        if (error == ScriptErrors.VersionConflict)
            return Conflict(ApiResponse<object>.FailureResponse("Conflict", new[] { error.Message }));

        return BadRequest(ApiResponse<object>.FailureResponse("Bad request", new[] { error.Message }));
    }
}

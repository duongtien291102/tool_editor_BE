using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Application.Features.Timelines;
using AiVideoStudio.Application.Features.Timelines.DTOs;
using AiVideoStudio.Api.Contracts.V1.Timelines.Requests;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AiVideoStudio.Api.Controllers.v1;

/// <summary>
/// Timeline management endpoints — create, query, mutate and autosave timelines.
/// Authorization: Owner of the project or an Admin role.
/// GET endpoints are also protected — the handler verifies ownership before returning data.
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize]
public class TimelinesController : ControllerBase
{
    private readonly IMediator _mediator;

    public TimelinesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ──────────────────────────────────────────────────────────────
    // Timeline CRUD
    // ──────────────────────────────────────────────────────────────

    /// <summary>Create a new Timeline for a project.</summary>
    /// <remarks>
    /// Each project can have at most ONE active timeline.
    /// If a timeline already exists for the given project a **409 Conflict** is returned.
    ///
    /// Example request body:
    /// ```json
    /// {
    ///   "projectId": "proj-abc123",
    ///   "name": "Main Cut",
    ///   "frameRate": 30.0,
    ///   "resolutionWidth": 1920,
    ///   "resolutionHeight": 1080
    /// }
    /// ```
    /// </remarks>
    [HttpPost("timelines")]
    [ProducesResponseType(typeof(ApiResponse<TimelineDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTimeline([FromBody] CreateTimelineRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateTimelineCommand(request.ProjectId, request.Name, request.FrameRate, request.ResolutionWidth, request.ResolutionHeight);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Created($"/api/v1/timelines/{result.Value!.Id}", ApiResponse<TimelineDto>.SuccessResponse(result.Value));
    }

    /// <summary>Get a Timeline by its unique ID.</summary>
    /// <remarks>
    /// Authorization is enforced: only the project owner or an Admin can retrieve this timeline.
    /// Soft-deleted timelines are excluded automatically at the repository level.
    /// </remarks>
    [HttpGet("timelines/{id}")]
    [ProducesResponseType(typeof(ApiResponse<TimelineDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTimelineById(string id, CancellationToken cancellationToken)
    {
        var query = new GetTimelineQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Ok(ApiResponse<TimelineDto>.SuccessResponse(result.Value!));
    }

    /// <summary>Get the Timeline associated with a project.</summary>
    /// <remarks>
    /// Authorization is enforced for GET: only the project owner or an Admin can read this.
    ///
    /// Example: `GET /api/v1/projects/proj-abc123/timeline`
    /// </remarks>
    [HttpGet("projects/{projectId}/timeline")]
    [ProducesResponseType(typeof(ApiResponse<TimelineDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTimelineByProject(string projectId, CancellationToken cancellationToken)
    {
        var query = new GetTimelineByProjectQuery(projectId);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Ok(ApiResponse<TimelineDto>.SuccessResponse(result.Value!));
    }

    /// <summary>Update the name and/or settings of a Timeline.</summary>
    /// <remarks>
    /// Example request body:
    /// ```json
    /// {
    ///   "name": "Director's Cut",
    ///   "frameRate": 24.0,
    ///   "resolutionWidth": 3840,
    ///   "resolutionHeight": 2160
    /// }
    /// ```
    /// Returns **409 Conflict** if a concurrent modification was detected (OCC).
    /// </remarks>
    [HttpPut("timelines/{id}")]
    [ProducesResponseType(typeof(ApiResponse<TimelineDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateTimeline(string id, [FromBody] UpdateTimelineRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateTimelineCommand(id, request.Name, request.FrameRate, request.ResolutionWidth, request.ResolutionHeight);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Ok(ApiResponse<TimelineDto>.SuccessResponse(result.Value!));
    }

    /// <summary>Soft-delete a Timeline.</summary>
    /// <remarks>
    /// The timeline is not physically removed; its `deletedAt` field is set instead.
    /// Returns **204 No Content** on success (REST convention for destructive operations).
    /// Returns **409 Conflict** on OCC version mismatch.
    /// </remarks>
    [HttpDelete("timelines/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteTimeline(string id, CancellationToken cancellationToken)
    {
        var command = new DeleteTimelineCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return NoContent(); // 204 — REST convention for DELETE
    }

    // ──────────────────────────────────────────────────────────────
    // Track operations
    // ──────────────────────────────────────────────────────────────

    /// <summary>Add a new Track to a Timeline.</summary>
    /// <remarks>
    /// Example request body:
    /// ```json
    /// {
    ///   "name": "Video Track 1",
    ///   "trackType": 0
    /// }
    /// ```
    /// `trackType`: 0 = Video, 1 = Audio, 2 = Subtitle (enum values from `TrackType`).
    /// Returns **409 Conflict** on OCC version mismatch.
    /// </remarks>
    [HttpPost("timelines/{id}/tracks")]
    [ProducesResponseType(typeof(ApiResponse<TrackDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddTrack(string id, [FromBody] AddTrackRequest request, CancellationToken cancellationToken)
    {
        var command = new AddTrackCommand(id, request.Name, request.TrackType);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Created($"/api/v1/timelines/{id}/tracks/{result.Value!.Id}", ApiResponse<TrackDto>.SuccessResponse(result.Value));
    }

    /// <summary>Remove a Track from a Timeline.</summary>
    /// <remarks>
    /// A track must be empty (no clips) before it can be deleted.
    /// Returns **409 Conflict** if the track still contains clips or on OCC mismatch.
    /// Returns **204 No Content** on success.
    /// </remarks>
    [HttpDelete("timelines/{id}/tracks/{trackId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteTrack(string id, string trackId, CancellationToken cancellationToken)
    {
        var command = new RemoveTrackCommand(id, trackId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return NoContent(); // 204 — REST convention for DELETE
    }

    /// <summary>Reorder a Track within a Timeline.</summary>
    /// <remarks>
    /// Example request body:
    /// ```json
    /// {
    ///   "newOrder": 2
    /// }
    /// ```
    /// Pass `trackId` as a query parameter: `PUT /api/v1/timelines/{id}/tracks/reorder?trackId=track-xyz`.
    /// Returns **409 Conflict** on OCC version mismatch.
    /// </remarks>
    [HttpPut("timelines/{id}/tracks/reorder")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReorderTrack(string id, [FromBody] ReorderTrackRequest request, [FromQuery] string trackId, CancellationToken cancellationToken)
    {
        var command = new ReorderTrackCommand(id, trackId, request.NewOrder);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Ok(ApiResponse<object>.SuccessResponse(null, "Track reordered."));
    }

    // ──────────────────────────────────────────────────────────────
    // Clip operations
    // ──────────────────────────────────────────────────────────────

    /// <summary>Add a new Clip to a Track inside a Timeline.</summary>
    /// <remarks>
    /// Example request body:
    /// ```json
    /// {
    ///   "trackId": "track-xyz",
    ///   "assetId": "asset-abc",
    ///   "startFrame": "00:00:00",
    ///   "endFrame": "00:00:05",
    ///   "name": "Opening scene",
    ///   "scriptSceneId": null,
    ///   "metadata": null
    /// }
    /// ```
    /// Returns **409 Conflict** if the clip overlaps another clip on the same track (for exclusive track types) or OCC mismatch.
    /// </remarks>
    [HttpPost("timelines/{id}/clips")]
    [ProducesResponseType(typeof(ApiResponse<ClipDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddClip(string id, [FromBody] AddClipRequest request, CancellationToken cancellationToken)
    {
        var command = new AddClipCommand(id, request.TrackId, request.AssetId, request.StartFrame, request.EndFrame, request.Name, request.ScriptSceneId, request.Metadata);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Created($"/api/v1/timelines/{id}/clips/{result.Value!.Id}", ApiResponse<ClipDto>.SuccessResponse(result.Value));
    }

    /// <summary>Update properties of an existing Clip (name, layer, speed, trim, volume, metadata).</summary>
    /// <remarks>
    /// Example request body:
    /// ```json
    /// {
    ///   "name": "Updated Clip Name",
    ///   "layer": 0,
    ///   "speed": 1.0,
    ///   "trimStart": "00:00:00",
    ///   "trimEnd": "00:00:00",
    ///   "volume": 1.0,
    ///   "metadata": null
    /// }
    /// ```
    /// </remarks>
    [HttpPut("timelines/{id}/clips/{clipId}")]
    [ProducesResponseType(typeof(ApiResponse<ClipDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateClip(string id, string clipId, [FromBody] UpdateClipRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateClipCommand(id, clipId, request.Name, request.Layer, request.Speed, request.TrimStart, request.TrimEnd, request.Volume, request.Metadata);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Ok(ApiResponse<ClipDto>.SuccessResponse(result.Value!));
    }

    /// <summary>Move a Clip to a different Track and/or position.</summary>
    /// <remarks>
    /// Example request body:
    /// ```json
    /// {
    ///   "newTrackId": "track-new",
    ///   "newStartFrame": "00:00:10",
    ///   "newEndFrame": "00:00:15"
    /// }
    /// ```
    /// Returns **409 Conflict** if the target track already has a clip in the specified range (for exclusive track types).
    /// </remarks>
    [HttpPut("timelines/{id}/clips/{clipId}/move")]
    [ProducesResponseType(typeof(ApiResponse<ClipDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MoveClip(string id, string clipId, [FromBody] MoveClipRequest request, CancellationToken cancellationToken)
    {
        var command = new MoveClipCommand(id, clipId, request.NewTrackId, request.NewStartFrame, request.NewEndFrame);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Ok(ApiResponse<ClipDto>.SuccessResponse(result.Value!));
    }

    /// <summary>Resize (change start/end frame) of a Clip in place.</summary>
    /// <remarks>
    /// Example request body:
    /// ```json
    /// {
    ///   "newStartFrame": "00:00:02",
    ///   "newEndFrame": "00:00:08"
    /// }
    /// ```
    /// </remarks>
    [HttpPut("timelines/{id}/clips/{clipId}/resize")]
    [ProducesResponseType(typeof(ApiResponse<ClipDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResizeClip(string id, string clipId, [FromBody] ResizeClipRequest request, CancellationToken cancellationToken)
    {
        var command = new ResizeClipCommand(id, clipId, request.NewStartFrame, request.NewEndFrame);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Ok(ApiResponse<ClipDto>.SuccessResponse(result.Value!));
    }

    /// <summary>Delete a Clip from a Timeline.</summary>
    /// <remarks>
    /// Returns **204 No Content** on success.
    /// </remarks>
    [HttpDelete("timelines/{id}/clips/{clipId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteClip(string id, string clipId, CancellationToken cancellationToken)
    {
        var command = new DeleteClipCommand(id, clipId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return NoContent(); // 204 — REST convention for DELETE
    }

    // ──────────────────────────────────────────────────────────────
    // AutoSave
    // ──────────────────────────────────────────────────────────────

    /// <summary>Auto-save the full state of a Timeline (idempotent).</summary>
    /// <remarks>
    /// This endpoint is **idempotent**: if the submitted `data` is identical to the persisted state,
    /// the version is NOT incremented and NO write is performed.
    ///
    /// The handler compares the full serialised DTO before deciding to persist.
    ///
    /// Example request body:
    /// ```json
    /// {
    ///   "data": {
    ///     "id": "timeline-001",
    ///     "version": 5,
    ///     "name": "Main Cut",
    ///     "tracks": []
    ///   }
    /// }
    /// ```
    /// Returns **409 Conflict** if the client's `version` does not match the server version (OCC).
    /// </remarks>
    [HttpPost("timelines/{id}/autosave")]
    [ProducesResponseType(typeof(ApiResponse<TimelineDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AutoSaveTimeline(string id, [FromBody] AutoSaveTimelineRequest request, CancellationToken cancellationToken)
    {
        var command = new AutoSaveTimelineCommand(id, request.Data);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return CreateErrorResponse(result.Error);

        return Ok(ApiResponse<TimelineDto>.SuccessResponse(result.Value!));
    }

    // ──────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Maps domain errors to proper HTTP status codes.
    /// All responses use ApiResponse&lt;T&gt; — no raw strings or untyped IActionResult.
    /// No exception swallowing here; only domain-level Result failures are handled.
    /// </summary>
    private IActionResult CreateErrorResponse(Error error)
    {
        // 404 Not Found
        if (error == TimelineErrors.NotFound || error.Code == "Project.NotFound")
            return NotFound(ApiResponse<object>.FailureResponse("Resource not found.", new[] { error.Message }, error.Code));

        // 401 Unauthorized (unauthenticated / missing token)
        if (error == AuthErrors.Unauthorized)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized.", new[] { error.Message }, error.Code));

        // 409 Conflict (OCC, business rule violations)
        if (error == TimelineErrors.VersionConflict
            || error == TimelineErrors.AlreadyExists
            || error == TimelineErrors.TrackContainsClips
            || error == TimelineErrors.ClipOverlap)
            return Conflict(ApiResponse<object>.FailureResponse("Conflict.", new[] { error.Message }, error.Code));

        // 400 Bad Request (everything else)
        return BadRequest(ApiResponse<object>.FailureResponse("Bad request.", new[] { error.Message }, error.Code));
    }
}

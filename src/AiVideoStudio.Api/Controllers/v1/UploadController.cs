using AiVideoStudio.Application.Features.Uploads;
using AiVideoStudio.Application.Features.Uploads.DTOs;
using AiVideoStudio.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace AiVideoStudio.Api.Controllers.v1;

[ApiController]
[Authorize]
[Route("api/v1/uploads")]
public sealed class UploadController : ControllerBase
{
    private readonly IMediator _mediator; public UploadController(IMediator mediator) => _mediator = mediator;
    [HttpPost("start")]
    [ProducesResponseType(typeof(ApiResponse<UploadSessionDto>), 201)]
    public async Task<IActionResult> Start(StartUploadRequest r, CancellationToken ct) { var x = await _mediator.Send(new StartUploadCommand(r.ProjectId, r.FileName, r.ContentType, r.FileSize, r.ChunkCount, r.Checksum), ct); return x.IsSuccess ? StatusCode(201, ApiResponse<UploadSessionDto>.SuccessResponse(x.Value!)) : Failure(x); }
    [HttpPost("{id}/chunk")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<ChunkDto>), 200)]
    public async Task<IActionResult> Chunk(string id, [FromForm] UploadChunkRequest r, CancellationToken ct) { if (r.Chunk is null || r.Chunk.Length == 0) return BadRequest(); await using var stream = r.Chunk.OpenReadStream(); using var memory = new MemoryStream(); await stream.CopyToAsync(memory, ct); var x = await _mediator.Send(new UploadChunkCommand(id, r.ChunkIndex, memory.ToArray(), r.Checksum), ct); return x.IsSuccess ? Ok(ApiResponse<ChunkDto>.SuccessResponse(x.Value!)) : Failure(x); }
    [HttpPost("{id}/complete")][ProducesResponseType(typeof(ApiResponse<UploadSessionDto>), 200)] public async Task<IActionResult> Complete(string id, CancellationToken ct) { var x = await _mediator.Send(new CompleteUploadCommand(id), ct); return x.IsSuccess ? Ok(ApiResponse<UploadSessionDto>.SuccessResponse(x.Value!)) : Failure(x); }
    [HttpPost("{id}/cancel")][ProducesResponseType(typeof(ApiResponse<object>), 200)] public async Task<IActionResult> Cancel(string id, CancellationToken ct) { var x = await _mediator.Send(new CancelUploadCommand(id), ct); return x.IsSuccess ? Ok(ApiResponse<object>.SuccessResponse(null!, "Upload cancelled.")) : Failure(x); }
    [HttpPost("{id}/retry")][ProducesResponseType(typeof(ApiResponse<UploadSessionDto>), 200)] public async Task<IActionResult> Retry(string id, CancellationToken ct) { var x = await _mediator.Send(new RetryUploadCommand(id), ct); return x.IsSuccess ? Ok(ApiResponse<UploadSessionDto>.SuccessResponse(x.Value!)) : Failure(x); }
    [HttpGet("{id}")][ProducesResponseType(typeof(ApiResponse<UploadSessionDto>), 200)] public async Task<IActionResult> Get(string id, CancellationToken ct) { var x = await _mediator.Send(new GetUploadSessionQuery(id), ct); return x.IsSuccess ? Ok(ApiResponse<UploadSessionDto>.SuccessResponse(x.Value!)) : Failure(x); }
    [HttpGet("/api/v1/projects/{projectId}/uploads")][ProducesResponseType(typeof(ApiResponse<PagedResult<UploadSummaryDto>>), 200)] public async Task<IActionResult> List(string projectId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) { var x = await _mediator.Send(new GetProjectUploadsQuery(projectId, page, pageSize), ct); return x.IsSuccess ? Ok(ApiResponse<PagedResult<UploadSummaryDto>>.SuccessResponse(x.Value!)) : Failure(x); }
    private IActionResult Failure(Result x) { var body = ApiResponse<object>.FailureResponse(x.Error.Message, x.ValidationErrors, x.Error.Code); if (x.Error.Code.Contains("Unauthorized")) return Unauthorized(body); if (x.Error.Code.Contains("Forbidden")) return StatusCode(403, body); if (x.Error.Code.Contains("NotFound")) return NotFound(body); return BadRequest(body); }
}
public sealed class StartUploadRequest { public string ProjectId { get; set; } = string.Empty; public string FileName { get; set; } = string.Empty; public string ContentType { get; set; } = string.Empty; public long FileSize { get; set; } public int ChunkCount { get; set; } public string Checksum { get; set; } = string.Empty; }
public sealed class UploadChunkRequest { public int ChunkIndex { get; set; } public string Checksum { get; set; } = string.Empty; public IFormFile? Chunk { get; set; } }

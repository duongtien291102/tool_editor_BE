using AiVideoStudio.Application.Features.Operations;
using AiVideoStudio.Application.Features.Operations.DTOs;
using AiVideoStudio.Application.Interfaces.Operations;
using AiVideoStudio.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiVideoStudio.Api.Controllers.v1;

[ApiController]
[Route("api/v1/system")]
public sealed class OperationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHealthCheckService _health;
    private readonly IMetricsCollector _metrics;
    public OperationsController(IMediator mediator,IHealthCheckService health,IMetricsCollector metrics){_mediator=mediator;_health=health;_metrics=metrics;}

    /// <summary>Return the complete production dependency health report.</summary>
    [AllowAnonymous]
    [HttpGet("health")]
    public async Task<IActionResult> Health(CancellationToken ct){var x=await _health.CheckAsync(true,ct);return StatusCode(x.Healthy?200:503,ApiResponse<SystemHealth>.SuccessResponse(x));}

    /// <summary>Return readiness for traffic, including external dependencies.</summary>
    [AllowAnonymous]
    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken ct){var x=await _health.CheckAsync(true,ct);return StatusCode(x.Healthy?200:503,ApiResponse<SystemHealth>.SuccessResponse(x));}

    /// <summary>Return process liveness without external dependency checks.</summary>
    [AllowAnonymous]
    [HttpGet("live")]
    public async Task<IActionResult> Live(CancellationToken ct){var x=await _health.CheckAsync(false,ct);return Ok(ApiResponse<SystemHealth>.SuccessResponse(x));}

    /// <summary>Return redacted runtime configuration records.</summary>
    [Authorize(Roles="Admin,Administrator")]
    [HttpGet("configuration")]
    public async Task<IActionResult> Configuration(CancellationToken ct){var x=await _mediator.Send(new GetSystemConfigurationQuery(),ct);return x.IsSuccess?Ok(ApiResponse<IReadOnlyList<ConfigurationDto>>.SuccessResponse(x.Value!)):Failure(x);}

    /// <summary>Update a dynamic system configuration value.</summary>
    [Authorize(Roles="Admin,Administrator")]
    [HttpPut("configuration")]
    public async Task<IActionResult> Configuration(UpdateConfigurationRequest r,CancellationToken ct){var x=await _mediator.Send(new UpdateSystemConfigurationCommand(r.Key,r.Value,r.IsSensitive),ct);return x.IsSuccess?Ok(ApiResponse<ConfigurationDto>.SuccessResponse(x.Value!)):Failure(x);}

    /// <summary>Return the in-process operational metrics snapshot.</summary>
    [Authorize(Roles="Admin,Administrator")]
    [HttpGet("metrics")]
    public IActionResult Metrics()=>Ok(ApiResponse<IReadOnlyDictionary<string,double>>.SuccessResponse(_metrics.Snapshot()));

    /// <summary>Return paginated audit history.</summary>
    [Authorize(Roles="Admin,Administrator")]
    [HttpGet("audit")]
    public async Task<IActionResult> Audit([FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken ct=default){var x=await _mediator.Send(new GetAuditLogsQuery(page,pageSize),ct);return x.IsSuccess?Ok(ApiResponse<PagedResult<AuditLogDto>>.SuccessResponse(x.Value!)):Failure(x);}

    /// <summary>Return notifications owned by the current user.</summary>
    [Authorize]
    [HttpGet("notifications")]
    public async Task<IActionResult> Notifications([FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken ct=default){var x=await _mediator.Send(new GetNotificationsQuery(page,pageSize),ct);return x.IsSuccess?Ok(ApiResponse<PagedResult<NotificationDto>>.SuccessResponse(x.Value!)):Failure(x);}

    /// <summary>Start retention and temporary-file maintenance.</summary>
    [Authorize(Roles="Admin,Administrator")]
    [HttpPost("maintenance")]
    public async Task<IActionResult> Maintenance(RunMaintenanceRequest r,CancellationToken ct){var x=await _mediator.Send(new RunMaintenanceCommand(r.Name),ct);return x.IsSuccess?Accepted(ApiResponse<MaintenanceDto>.SuccessResponse(x.Value!)):Failure(x);}

    private IActionResult Failure(Result x){var body=ApiResponse<object>.FailureResponse(x.Error.Message,x.ValidationErrors,x.Error.Code);if(x.Error.Code.Contains("Unauthorized"))return Unauthorized(body);if(x.Error.Code.Contains("Forbidden"))return StatusCode(403,body);if(x.Error.Code.Contains("NotFound"))return NotFound(body);return BadRequest(body);}
}

public sealed class UpdateConfigurationRequest{public string Key{get;set;}=string.Empty;public string Value{get;set;}=string.Empty;public bool IsSensitive{get;set;}}
public sealed class RunMaintenanceRequest{public string Name{get;set;}="retention";}

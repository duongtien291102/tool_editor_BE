using System.Security.Claims;
using AiVideoStudio.Application.Features.OperationsAdmin.Commands;
using AiVideoStudio.Application.Interfaces.OperationsAdmin;
using AiVideoStudio.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiVideoStudio.Api.Controllers.v1;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/v1/admin")]
public sealed class PlatformAdministrationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogExplorerService _logExplorer;
    private readonly IMetricsExplorerService _metricsExplorer;
    private readonly ITraceExplorerService _traceExplorer;
    private readonly IPlatformAdministrationService _adminService;

    public PlatformAdministrationController(
        IMediator mediator,
        ILogExplorerService logExplorer,
        IMetricsExplorerService metricsExplorer,
        ITraceExplorerService traceExplorer,
        IPlatformAdministrationService adminService)
    {
        _mediator = mediator;
        _logExplorer = logExplorer;
        _metricsExplorer = metricsExplorer;
        _traceExplorer = traceExplorer;
        _adminService = adminService;
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOperationsDashboardQuery(), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOperationsDashboardQuery(), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("configuration")]
    public async Task<IActionResult> GetConfiguration(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPlatformConfigurationQuery(), cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("configuration")]
    public async Task<IActionResult> UpdateConfiguration([FromBody] UpdatePlatformConfigurationCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("licenses")]
    public async Task<IActionResult> GetLicense([FromQuery] string tenantId = "default", CancellationToken cancellationToken = default)
    {
        var license = await _adminService.GetLicenseAsync(tenantId, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(license!));
    }

    [HttpPut("licenses")]
    public async Task<IActionResult> UpdateLicense([FromBody] UpdateLicenseRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin";
        await _adminService.UpdateLicenseAsync(request.TenantId, request.LicenseKey, request.LicenseType, request.MaxSeats, userId, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { Message = "License updated successfully." }));
    }

    [HttpGet("incidents")]
    public async Task<IActionResult> GetIncidents([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetIncidentsQuery(status), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("incidents")]
    public async Task<IActionResult> CreateIncident([FromBody] CreateIncidentCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("incidents/{id}")]
    public async Task<IActionResult> ResolveIncident(string id, [FromBody] ResolveIncidentRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin";
        var command = new ResolveIncidentCommand(id, request.RootCause, request.Resolution, userId);
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("audit")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAuditLogsQuery(skip, take), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] string? query, [FromQuery] string? correlationId, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var logs = await _logExplorer.SearchLogsAsync(query, correlationId, limit, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(logs));
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics([FromQuery] string? metricName, CancellationToken cancellationToken = default)
    {
        var metrics = await _metricsExplorer.QueryMetricsAsync(metricName, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(metrics));
    }

    [HttpGet("traces")]
    public async Task<IActionResult> GetTraces([FromQuery] string? traceId, CancellationToken cancellationToken = default)
    {
        var traces = await _traceExplorer.SearchTracesAsync(traceId, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(traces));
    }

    [HttpPost("backup")]
    public async Task<IActionResult> CreateBackup([FromBody] CreateBackupCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("restore")]
    public async Task<IActionResult> RestoreBackup([FromBody] RestoreBackupCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("maintenance")]
    public async Task<IActionResult> ScheduleMaintenance([FromBody] ScheduleMaintenanceCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("replay")]
    public async Task<IActionResult> ReplayJob([FromBody] ReplayJobCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("feature-flags")]
    public async Task<IActionResult> GetFeatureFlags(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFeatureFlagsQuery(), cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("feature-flags")]
    public async Task<IActionResult> SetFeatureFlag([FromBody] SetFeatureFlagCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    private IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(ApiResponse<T>.SuccessResponse(result.Value));

        var body = ApiResponse<object>.FailureResponse(result.Error.Message, result.ValidationErrors, result.Error.Code);
        if (result.Error.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase)) return NotFound(body);
        if (result.Error.Code.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase)) return Unauthorized(body);
        return BadRequest(body);
    }
}

public record UpdateLicenseRequest(string TenantId, string LicenseKey, string LicenseType, int MaxSeats);
public record ResolveIncidentRequest(string RootCause, string Resolution);

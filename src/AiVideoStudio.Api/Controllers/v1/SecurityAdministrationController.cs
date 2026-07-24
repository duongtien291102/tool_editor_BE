using System.Security.Claims;
using AiVideoStudio.Application.Features.SecurityGovernance.Commands;
using AiVideoStudio.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiVideoStudio.Api.Controllers.v1;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/v1/security")]
public sealed class SecurityAdministrationController : ControllerBase
{
    private readonly IMediator _mediator;

    public SecurityAdministrationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSecurityDashboardQuery(), ct);
        return HandleResult(result);
    }

    [HttpGet("incidents")]
    public async Task<IActionResult> GetIncidents([FromQuery] string? status, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSecurityIncidentsQuery(status), ct);
        return HandleResult(result);
    }

    [HttpPost("incidents")]
    public async Task<IActionResult> CreateIncident([FromBody] CreateSecurityIncidentCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }

    [HttpGet("policies")]
    public async Task<IActionResult> GetPolicy(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSecurityPolicyQuery(), ct);
        return HandleResult(result);
    }

    [HttpPut("policies")]
    public async Task<IActionResult> UpdatePolicy([FromBody] UpdateSecurityPolicyCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }

    [HttpGet("compliance")]
    public async Task<IActionResult> GetComplianceReports([FromQuery] string? frameworkType, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetComplianceReportsQuery(frameworkType), ct);
        return HandleResult(result);
    }

    [HttpPost("compliance/generate")]
    public async Task<IActionResult> GenerateComplianceReport([FromBody] GenerateComplianceRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin";
        var result = await _mediator.Send(new GenerateComplianceReportCommand(request.FrameworkType, userId), ct);
        return HandleResult(result);
    }

    [HttpGet("risk")]
    public async Task<IActionResult> AssessRisk([FromQuery] string userId, [FromQuery] string? deviceFingerprint, CancellationToken ct)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var userAgent = Request.Headers.UserAgent.ToString();
        var result = await _mediator.Send(new AssessRiskQuery(userId, clientIp, userAgent, deviceFingerprint), ct);
        return HandleResult(result);
    }

    [HttpGet("devices")]
    public async Task<IActionResult> GetDevices([FromQuery] string userId, CancellationToken ct)
    {
        var result = await _mediator.Send(new AssessRiskQuery(userId, "127.0.0.1", "Internal", null), ct);
        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    [HttpPost("devices/trust")]
    public async Task<IActionResult> TrustDevice([FromBody] TrustDeviceCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }

    [HttpDelete("devices/{id}")]
    public async Task<IActionResult> RevokeDevice(string id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin";
        var result = await _mediator.Send(new RevokeDeviceCommand(id, userId), ct);
        return HandleResult(result);
    }

    [HttpGet("secrets")]
    public async Task<IActionResult> GetSecrets(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSecretMetadataQuery(), ct);
        return HandleResult(result);
    }

    [HttpPost("secrets/rotate")]
    public async Task<IActionResult> RotateSecret([FromBody] RotateSecretRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin";
        var result = await _mediator.Send(new RotateSecretCommand(request.KeyName, userId), ct);
        return HandleResult(result);
    }

    [HttpGet("rate-limits")]
    public async Task<IActionResult> GetRateLimits(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRateLimitPoliciesQuery(), ct);
        return HandleResult(result);
    }

    [HttpPut("rate-limits")]
    public async Task<IActionResult> UpdateRateLimit([FromBody] UpdateRateLimitPolicyCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
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

public record GenerateComplianceRequest(string FrameworkType);
public record RotateSecretRequest(string KeyName);

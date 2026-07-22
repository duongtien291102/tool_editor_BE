using AiVideoStudio.Application.Features.Auth.Commands;
using AiVideoStudio.Shared.ApiContracts.V1.Auth.Requests;
using AiVideoStudio.Shared.ApiContracts.V1.Auth.Responses;
using AiVideoStudio.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(
            request.Username,
            request.Password,
            request.DeviceId,
            Request.Headers["User-Agent"].ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
        );

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            SetRefreshTokenCookie(result.Value.RefreshToken);
            
            // Do not expose refresh token in the body
            var safeResponse = new AuthResponse(result.Value.AccessToken, result.Value.ExpiresIn, null);
            return Ok(ApiResponse<AuthResponse>.SuccessResponse(safeResponse));
        }

        return BadRequest(ApiResponse<AuthResponse>.FailureResponse(result.Error!.Message, new[] { result.Error.Message }, result.Error.Code));
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(
            request.Username,
            request.Email,
            request.Password,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            request.DeviceId,
            Request.Headers["User-Agent"].ToString()
        );

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            SetRefreshTokenCookie(result.Value.RefreshToken);
            
            var safeResponse = new AuthResponse(result.Value.AccessToken, result.Value.ExpiresIn, null);
            return Ok(ApiResponse<AuthResponse>.SuccessResponse(safeResponse));
        }

        if (result.Error?.Code == "USER.EMAIL_EXISTS" || result.Error?.Code == "USER.USERNAME_EXISTS")
            return Conflict(ApiResponse<AuthResponse>.FailureResponse(result.Error.Message, new[] { result.Error.Message }, result.Error.Code));

        return BadRequest(ApiResponse<AuthResponse>.FailureResponse(result.Error!.Message, new[] { result.Error.Message }, result.Error.Code));
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<ApiResponse<bool>>> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var command = new VerifyEmailCommand(
            request.Token,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
        );

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
            return Ok(ApiResponse<bool>.SuccessResponse(true));

        return BadRequest(ApiResponse<bool>.FailureResponse(result.Error!.Message, new[] { result.Error.Message }, result.Error.Code));
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var command = new ForgotPasswordCommand(
            request.Email,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
        );

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
            return Ok(ApiResponse<bool>.SuccessResponse(true));

        return BadRequest(ApiResponse<bool>.FailureResponse(result.Error!.Message, new[] { result.Error.Message }, result.Error.Code));
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var command = new ResetPasswordCommand(
            request.Token,
            request.NewPassword,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
        );

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
            return Ok(ApiResponse<bool>.SuccessResponse(true));

        return BadRequest(ApiResponse<bool>.FailureResponse(result.Error!.Message, new[] { result.Error.Message }, result.Error.Code));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(ApiResponse<AuthResponse>.FailureResponse("Refresh token is missing.", new[] { "Refresh token is missing." }, "AUTH.MISSING_TOKEN"));
        }

        var command = new RefreshTokenCommand(
            refreshToken,
            request.DeviceId,
            Request.Headers["User-Agent"].ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
        );

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            SetRefreshTokenCookie(result.Value.RefreshToken);
            var safeResponse = new AuthResponse(result.Value.AccessToken, result.Value.ExpiresIn, null);
            return Ok(ApiResponse<AuthResponse>.SuccessResponse(safeResponse));
        }

        return Unauthorized(ApiResponse<AuthResponse>.FailureResponse(result.Error!.Message, new[] { result.Error.Message }, result.Error.Code));
    }

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<bool>>> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var command = new LogoutCommand(
                refreshToken,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
            );
            await _mediator.Send(command, cancellationToken);
        }

        Response.Cookies.Delete("refreshToken");
        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }

    private void SetRefreshTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Use true in production
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(7) // Should match options
        };
        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }
}



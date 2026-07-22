using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Shared.ApiContracts.V1.Auth.Responses;
using AiVideoStudio.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace AiVideoStudio.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ICurrentUser _currentUser;
    private readonly IMediator _mediator;

    public UsersController(ICurrentUser currentUser, IMediator mediator)
    {
        _currentUser = currentUser;
        _mediator = mediator;
    }

    [HttpGet("me")]
    public ActionResult<ApiResponse<UserResponse>> GetMe()
    {
        var response = new UserResponse(
            _currentUser.UserId ?? string.Empty,
            _currentUser.Username ?? string.Empty,
            string.Empty, // Email not in token, could fetch from DB if needed
            "Active",
            _currentUser.Roles
        );

        return Ok(ApiResponse<UserResponse>.SuccessResponse(response));
    }

    [HttpPut("me/profile")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateProfile([FromBody] AiVideoStudio.Shared.ApiContracts.V1.Auth.Requests.UpdateProfileRequest request, System.Threading.CancellationToken cancellationToken)
    {
        var command = new AiVideoStudio.Application.Features.Auth.Commands.UpdateProfileCommand(
            _currentUser.UserId ?? string.Empty,
            request.Username,
            request.Version
        );

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
            return Ok(ApiResponse<bool>.SuccessResponse(true));

        if (result.Error?.Code == "GENERAL.CONCURRENCY_EXCEPTION")
            return Conflict(ApiResponse<bool>.FailureResponse(result.Error.Message, new[] { result.Error.Message }, result.Error.Code));

        return BadRequest(ApiResponse<bool>.FailureResponse(result.Error!.Message, new[] { result.Error.Message }, result.Error.Code));
    }

    [HttpPut("me/password")]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] AiVideoStudio.Shared.ApiContracts.V1.Auth.Requests.ChangePasswordRequest request, System.Threading.CancellationToken cancellationToken)
    {
        var command = new AiVideoStudio.Application.Features.Auth.Commands.ChangePasswordCommand(
            _currentUser.UserId ?? string.Empty,
            request.CurrentPassword,
            request.NewPassword,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
        );

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
            return Ok(ApiResponse<bool>.SuccessResponse(true));

        return BadRequest(ApiResponse<bool>.FailureResponse(result.Error!.Message, new[] { result.Error.Message }, result.Error.Code));
    }
}

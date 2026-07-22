using AiVideoStudio.Application.Features.Auth.Commands;
using AiVideoStudio.Application.Interfaces.Auth;
using AiVideoStudio.Domain.Events.Auth;
using AiVideoStudio.Shared.ApiContracts.V1.Auth.Responses;
using AiVideoStudio.Shared.Logging;
using AiVideoStudio.Shared.Responses;
using AiVideoStudio.Application.Events;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Application.Features.Auth.Handlers;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IAuthenticationService _authService;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IPermissionResolver _permissionResolver;
    private readonly IEventBus _eventBus;
    private readonly IAppLogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IAuthenticationService authService,
        IJwtTokenGenerator jwtGenerator,
        IRefreshTokenService refreshTokenService,
        IPermissionResolver permissionResolver,
        IEventBus eventBus,
        IAppLogger<LoginCommandHandler> logger)
    {
        _authService = authService;
        _jwtGenerator = jwtGenerator;
        _refreshTokenService = refreshTokenService;
        _permissionResolver = permissionResolver;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var authResult = await _authService.VerifyCredentialsAsync(request.Username, request.Password, cancellationToken);
        if (!authResult.IsSuccess || authResult.Value == null)
        {
            _logger.LogWarning(0, $"[Security Audit] Login Failed for username: {request.Username}");
            return Result<AuthResponse>.Failure(authResult.Error!);
        }

        var user = authResult.Value;
        var roles = await _permissionResolver.GetRolesForUserAsync(user.Id, cancellationToken);

        var accessToken = _jwtGenerator.GenerateToken(user, roles);
        var refreshResult = await _refreshTokenService.GenerateRefreshTokenAsync(
            user.Id, string.Empty, request.ClientIp, request.DeviceId, request.UserAgent, cancellationToken);

        _logger.LogInformation(0, $"[Security Audit] Login Success for user: {user.Id}");

        await _eventBus.PublishAsync(new UserLoggedInEvent(user.Id, request.ClientIp), cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse(
            AccessToken: accessToken,
            ExpiresIn: 3600, // Depends on config, simplified here
            RefreshToken: refreshResult.PlainToken
        ));
    }
}


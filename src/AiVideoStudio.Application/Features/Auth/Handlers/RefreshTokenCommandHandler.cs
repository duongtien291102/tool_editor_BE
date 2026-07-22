using AiVideoStudio.Application.Features.Auth.Commands;
using AiVideoStudio.Application.Interfaces.Auth;
using AiVideoStudio.Domain.Events.Auth;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.ApiContracts.V1.Auth.Responses;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Logging;
using AiVideoStudio.Shared.Responses;
using AiVideoStudio.Application.Events;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Application.Features.Auth.Handlers;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly IPermissionResolver _permissionResolver;
    private readonly IEventBus _eventBus;
    private readonly IAppLogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IRefreshTokenService refreshTokenService,
        IJwtTokenGenerator jwtGenerator,
        IPermissionResolver permissionResolver,
        IEventBus eventBus,
        IAppLogger<RefreshTokenCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _refreshTokenService = refreshTokenService;
        _jwtGenerator = jwtGenerator;
        _permissionResolver = permissionResolver;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _refreshTokenService.HashToken(request.Token);
        var tokenEntity = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (tokenEntity == null)
        {
            return Result<AuthResponse>.Failure(AuthErrors.InvalidRefreshToken);
        }

        if (tokenEntity.IsUsed)
        {
            _logger.LogWarning(0, $"[Security Audit] Replay Attack Detected for Family: {tokenEntity.FamilyId}");
            await _refreshTokenRepository.RevokeFamilyAsync(tokenEntity.FamilyId, "Replay Attack Detected", request.ClientIp, cancellationToken);
            await _eventBus.PublishAsync(new RefreshTokenCompromisedEvent(tokenEntity.UserId, tokenEntity.FamilyId, request.ClientIp), cancellationToken);
            return Result<AuthResponse>.Failure(AuthErrors.InvalidRefreshToken);
        }

        if (!tokenEntity.IsActive)
        {
            return Result<AuthResponse>.Failure(AuthErrors.InvalidRefreshToken);
        }

        // Mark token as used
        tokenEntity.IsUsed = true;
        await _refreshTokenRepository.UpdateAsync(tokenEntity, cancellationToken);

        var user = await _userRepository.GetByIdAsync(tokenEntity.UserId, cancellationToken);
        if (user == null || user.Status != Domain.Enums.UserStatus.Active)
        {
            return Result<AuthResponse>.Failure(AuthErrors.UserNotActive);
        }

        var roles = await _permissionResolver.GetRolesForUserAsync(user.Id, cancellationToken);
        var newAccessToken = _jwtGenerator.GenerateToken(user, roles);
        
        var newRefreshResult = await _refreshTokenService.GenerateRefreshTokenAsync(
            user.Id, tokenEntity.FamilyId, request.ClientIp, request.DeviceId, request.UserAgent, cancellationToken);

        tokenEntity.ReplacedByTokenHash = newRefreshResult.Entity.TokenHash;
        await _refreshTokenRepository.UpdateAsync(tokenEntity, cancellationToken);

        _logger.LogInformation(0, $"[Security Audit] Token Refreshed for user: {user.Id}");
        await _eventBus.PublishAsync(new RefreshTokenRotatedEvent(user.Id, tokenEntity.FamilyId), cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse(
            AccessToken: newAccessToken,
            ExpiresIn: 3600,
            RefreshToken: newRefreshResult.PlainToken
        ));
    }
}



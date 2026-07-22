using AiVideoStudio.Application.Features.Auth.Commands;
using AiVideoStudio.Application.Interfaces.Auth;
using AiVideoStudio.Domain.Events.Auth;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.Logging;
using AiVideoStudio.Shared.Responses;
using AiVideoStudio.Application.Events;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Application.Features.Auth.Handlers;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result<bool>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IEventBus _eventBus;
    private readonly IAppLogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenService refreshTokenService,
        IEventBus eventBus,
        IAppLogger<LogoutCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _refreshTokenService = refreshTokenService;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _refreshTokenService.HashToken(request.Token);
        var tokenEntity = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (tokenEntity != null && !tokenEntity.IsRevoked)
        {
            tokenEntity.IsRevoked = true;
            tokenEntity.ReasonRevoked = "Logout";
            tokenEntity.RevokedByIp = request.ClientIp;
            tokenEntity.RevokedAt = System.DateTimeOffset.UtcNow;
            await _refreshTokenRepository.UpdateAsync(tokenEntity, cancellationToken);

            _logger.LogInformation(0, $"[Security Audit] Logout for user: {tokenEntity.UserId}");
            await _eventBus.PublishAsync(new UserLoggedOutEvent(tokenEntity.UserId, request.ClientIp), cancellationToken);
        }

        return Result<bool>.Success(true);
    }
}



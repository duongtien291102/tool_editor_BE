using AiVideoStudio.Application.Features.Auth.Commands;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Application.Interfaces.Auth;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Application.Features.Auth.Handlers;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserTokenRepository _userTokenRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IPasswordHasher _passwordHasher;

    public VerifyEmailCommandHandler(
        IUserRepository userRepository,
        IUserTokenRepository userTokenRepository,
        ITransactionManager transactionManager,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _userTokenRepository = userTokenRepository;
        _transactionManager = transactionManager;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<bool>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _passwordHasher.HashPassword(request.Token);
        var userToken = await _userTokenRepository.GetByHashAsync(tokenHash, "EmailVerification", cancellationToken);

        if (userToken == null || userToken.IsUsed || userToken.RevokedAt != null || userToken.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return Result<bool>.Failure(AuthErrors.InvalidToken);
        }

        var user = await _userRepository.GetByIdAsync(userToken.UserId, cancellationToken);
        if (user == null)
            return Result<bool>.Failure(UserErrors.NotFound);

        user.Status = Domain.Enums.UserStatus.Active;
        user.EmailVerifiedAt = DateTimeOffset.UtcNow;
        // user.Version++; // Version increment is normally done in repository or application logic

        userToken.IsUsed = true;
        userToken.UsedByIp = request.ClientIp;

        try
        {
            await _transactionManager.BeginTransactionAsync(cancellationToken);

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _userTokenRepository.UpdateAsync(userToken, cancellationToken);

            await _transactionManager.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception)
        {
            await _transactionManager.AbortTransactionAsync(cancellationToken);
            throw;
        }

        return Result<bool>.Success(true);
    }
}

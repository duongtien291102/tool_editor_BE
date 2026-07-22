using AiVideoStudio.Application.Features.Auth.Commands;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Application.Interfaces.Auth;
using AiVideoStudio.Application.Interfaces.Auth;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Application.Features.Auth.Handlers;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHistoryRepository _passwordHistoryRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHistoryRepository passwordHistoryRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITransactionManager transactionManager,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHistoryRepository = passwordHistoryRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _transactionManager = transactionManager;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<bool>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return Result<bool>.Failure(UserErrors.NotFound);

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return Result<bool>.Failure(AuthErrors.InvalidCredentials);
        }

        // Check password history for reuse
        var recentPasswords = await _passwordHistoryRepository.GetRecentByUserIdAsync(user.Id, 5, cancellationToken);
        foreach (var history in recentPasswords)
        {
            if (_passwordHasher.VerifyPassword(request.NewPassword, history.PasswordHash))
            {
                return Result<bool>.Failure(AuthErrors.PasswordReuse);
            }
        }

        var newHashedPassword = _passwordHasher.HashPassword(request.NewPassword);
        user.PasswordHash = newHashedPassword;
        user.PasswordChangedAt = DateTimeOffset.UtcNow;
        // user.Version++;

        var newPasswordHistory = new PasswordHistory
        {
            UserId = user.Id,
            PasswordHash = newHashedPassword,
            Algorithm = "BCrypt",
            CostFactor = 11
        };

        try
        {
            await _transactionManager.BeginTransactionAsync(cancellationToken);

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _passwordHistoryRepository.AddAsync(newPasswordHistory, cancellationToken);

            // Revoke all existing refresh tokens for security
            await _refreshTokenRepository.RevokeAllForUserAsync(user.Id, "PasswordChanged", request.ClientIp, cancellationToken);

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

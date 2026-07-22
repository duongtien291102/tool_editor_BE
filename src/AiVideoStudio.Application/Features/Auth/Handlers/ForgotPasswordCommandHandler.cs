using AiVideoStudio.Application.Features.Auth.Commands;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Application.Interfaces.Auth;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.Responses;
using MediatR;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Application.Features.Auth.Handlers;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserTokenRepository _userTokenRepository;
    private readonly IEmailOutboxRepository _emailOutboxRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IPasswordHasher _passwordHasher;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IUserTokenRepository userTokenRepository,
        IEmailOutboxRepository emailOutboxRepository,
        ITransactionManager transactionManager,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _userTokenRepository = userTokenRepository;
        _emailOutboxRepository = emailOutboxRepository;
        _transactionManager = transactionManager;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<bool>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindByEmailAsync(request.Email, cancellationToken);
        
        // Anti-enumeration: always return success even if email not found
        if (user == null)
        {
            return Result<bool>.Success(true);
        }

        var resetToken = Guid.NewGuid().ToString("N");
        var tokenHash = _passwordHasher.HashPassword(resetToken);

        var userToken = new UserToken
        {
            UserId = user.Id,
            Purpose = "PasswordReset",
            TokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            CreatedByIp = request.ClientIp
        };

        var emailOutbox = new EmailOutbox
        {
            TemplateName = "PasswordResetEmail",
            Variables = JsonSerializer.Serialize(new { Email = user.Email, Token = resetToken }),
            NextRetryAt = DateTimeOffset.UtcNow,
            Priority = 1
        };

        try
        {
            await _transactionManager.BeginTransactionAsync(cancellationToken);

            await _userTokenRepository.AddAsync(userToken, cancellationToken);
            await _emailOutboxRepository.AddAsync(emailOutbox, cancellationToken);

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

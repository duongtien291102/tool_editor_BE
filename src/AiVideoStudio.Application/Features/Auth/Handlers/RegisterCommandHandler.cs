using AiVideoStudio.Application.Features.Auth.Commands;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Application.Interfaces.Auth;
using AiVideoStudio.Application.Interfaces.Auth;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.ApiContracts.V1.Auth.Responses;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using MediatR;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Application.Features.Auth.Handlers;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserTokenRepository _userTokenRepository;
    private readonly IPasswordHistoryRepository _passwordHistoryRepository;
    private readonly IEmailOutboxRepository _emailOutboxRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly IRefreshTokenService _refreshTokenService;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IUserTokenRepository userTokenRepository,
        IPasswordHistoryRepository passwordHistoryRepository,
        IEmailOutboxRepository emailOutboxRepository,
        IAuditLogRepository auditLogRepository,
        ITransactionManager transactionManager,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtGenerator,
        IRefreshTokenService refreshTokenService)
    {
        _userRepository = userRepository;
        _userTokenRepository = userTokenRepository;
        _passwordHistoryRepository = passwordHistoryRepository;
        _emailOutboxRepository = emailOutboxRepository;
        _auditLogRepository = auditLogRepository;
        _transactionManager = transactionManager;
        _passwordHasher = passwordHasher;
        _jwtGenerator = jwtGenerator;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsEmailAsync(request.Email, cancellationToken))
            return Result<AuthResponse>.Failure(UserErrors.EmailExists); // Custom error code like 409

        if (await _userRepository.ExistsUsernameAsync(request.Username, cancellationToken))
            return Result<AuthResponse>.Failure(UserErrors.UsernameExists);

        var hashedPassword = _passwordHasher.HashPassword(request.Password);
        
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = hashedPassword,
            Status = Domain.Enums.UserStatus.PendingVerification
        };

        var verificationToken = Guid.NewGuid().ToString("N");
        var tokenHash = _passwordHasher.HashPassword(verificationToken); // Fast hash for token

        var userToken = new UserToken
        {
            UserId = user.Id,
            Purpose = "EmailVerification",
            TokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            CreatedByIp = request.ClientIp
        };

        var passwordHistory = new PasswordHistory
        {
            UserId = user.Id,
            PasswordHash = hashedPassword,
            Algorithm = "BCrypt",
            CostFactor = 11
        };

        var emailOutbox = new EmailOutbox
        {
            TemplateName = "VerificationEmail",
            Variables = JsonSerializer.Serialize(new { Email = user.Email, Token = verificationToken }),
            NextRetryAt = DateTimeOffset.UtcNow,
            Priority = 1
        };

        var auditLog = new AuditLog
        {
            UserId = user.Id,
            Action = "UserRegistered",
            Result = "Success",
            IpAddress = request.ClientIp,
            Device = request.DeviceId,
            Browser = request.UserAgent
        };

        try
        {
            await _transactionManager.BeginTransactionAsync(cancellationToken);

            await _userRepository.AddAsync(user, cancellationToken);
            await _userTokenRepository.AddAsync(userToken, cancellationToken);
            await _passwordHistoryRepository.AddAsync(passwordHistory, cancellationToken);
            await _emailOutboxRepository.AddAsync(emailOutbox, cancellationToken);
            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            await _transactionManager.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception)
        {
            await _transactionManager.AbortTransactionAsync(cancellationToken);
            throw;
        }

        // Generate tokens
        var accessToken = _jwtGenerator.GenerateToken(user, new System.Collections.Generic.List<string>());
        var refreshResult = await _refreshTokenService.GenerateRefreshTokenAsync(
            user.Id, string.Empty, request.ClientIp, request.DeviceId, request.UserAgent, cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse(
            AccessToken: accessToken,
            ExpiresIn: 3600,
            RefreshToken: refreshResult.PlainToken
        ));
    }
}

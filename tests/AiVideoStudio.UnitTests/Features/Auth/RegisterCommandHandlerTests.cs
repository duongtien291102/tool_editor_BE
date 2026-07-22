using AiVideoStudio.Application.Features.Auth.Commands;
using AiVideoStudio.Application.Features.Auth.Handlers;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Application.Interfaces.Auth;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AiVideoStudio.UnitTests.Features.Auth;

public class RegisterCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserTokenRepository _userTokenRepository = Substitute.For<IUserTokenRepository>();
    private readonly IPasswordHistoryRepository _passwordHistoryRepository = Substitute.For<IPasswordHistoryRepository>();
    private readonly IEmailOutboxRepository _emailOutboxRepository = Substitute.For<IEmailOutboxRepository>();
    private readonly IAuditLogRepository _auditLogRepository = Substitute.For<IAuditLogRepository>();
    private readonly ITransactionManager _transactionManager = Substitute.For<ITransactionManager>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwtGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IRefreshTokenService _refreshTokenService = Substitute.For<IRefreshTokenService>();

    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _handler = new RegisterCommandHandler(
            _userRepository,
            _userTokenRepository,
            _passwordHistoryRepository,
            _emailOutboxRepository,
            _auditLogRepository,
            _transactionManager,
            _passwordHasher,
            _jwtGenerator,
            _refreshTokenService);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenRegistrationDataIsValid()
    {
        // Arrange
        var command = new RegisterCommand("testuser", "test@example.com", "Password123!", "127.0.0.1", "device1", "agent");
        var cancellationToken = CancellationToken.None;

        _userRepository.ExistsEmailAsync(command.Email, cancellationToken).Returns(false);
        _userRepository.ExistsUsernameAsync(command.Username, cancellationToken).Returns(false);
        _passwordHasher.HashPassword(Arg.Any<string>()).Returns("hashed_password");

        _jwtGenerator.GenerateToken(Arg.Any<User>(), Arg.Any<List<string>>()).Returns("access_token");

        var dummyRefreshTokenEntity = new RefreshToken
        {
            UserId = "user_id_1",
            TokenHash = "token_hash",
            FamilyId = "family_id_1"
        };
        _refreshTokenService.GenerateRefreshTokenAsync(Arg.Any<string>(), Arg.Any<string>(), command.ClientIp, command.DeviceId, command.UserAgent, cancellationToken)
            .Returns(new RefreshTokenResult("plain_refresh_token", dummyRefreshTokenEntity));

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("access_token");
        result.Value.RefreshToken.Should().Be("plain_refresh_token");

        await _userRepository.Received(1).AddAsync(Arg.Is<User>(u => u.Username == command.Username && u.Email == command.Email), cancellationToken);
        await _transactionManager.Received(1).BeginTransactionAsync(cancellationToken);
        await _transactionManager.Received(1).CommitTransactionAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenEmailAlreadyExists()
    {
        // Arrange
        var command = new RegisterCommand("testuser", "existing@example.com", "Password123!", "127.0.0.1", "device1", "agent");
        _userRepository.ExistsEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(UserErrors.EmailExists);
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUsernameAlreadyExists()
    {
        // Arrange
        var command = new RegisterCommand("existinguser", "test@example.com", "Password123!", "127.0.0.1", "device1", "agent");
        _userRepository.ExistsEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(false);
        _userRepository.ExistsUsernameAsync(command.Username, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(UserErrors.UsernameExists);
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldRollbackTransaction_WhenRepositoryThrowsException()
    {
        // Arrange
        var command = new RegisterCommand("testuser", "test@example.com", "Password123!", "127.0.0.1", "device1", "agent");
        var cancellationToken = CancellationToken.None;

        _userRepository.ExistsEmailAsync(command.Email, cancellationToken).Returns(false);
        _userRepository.ExistsUsernameAsync(command.Username, cancellationToken).Returns(false);
        _userRepository.AddAsync(Arg.Any<User>(), cancellationToken).ThrowsAsync(new InvalidOperationException("DB error"));

        // Act
        Func<Task> act = async () => await _handler.Handle(command, cancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DB error");
        await _transactionManager.Received(1).AbortTransactionAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldHashPassword_AndCreateAllRequiredEntities()
    {
        // Arrange
        var command = new RegisterCommand("newuser", "newuser@example.com", "Secret123!", "192.168.1.1", "device9", "Agent/1.0");
        var cancellationToken = CancellationToken.None;

        _userRepository.ExistsEmailAsync(command.Email, cancellationToken).Returns(false);
        _userRepository.ExistsUsernameAsync(command.Username, cancellationToken).Returns(false);
        _passwordHasher.HashPassword(Arg.Any<string>()).Returns("hashed_verification_token");
        _passwordHasher.HashPassword(command.Password).Returns("secure_hash_123");

        _jwtGenerator.GenerateToken(Arg.Any<User>(), Arg.Any<List<string>>()).Returns("jwt_token");
        _refreshTokenService.GenerateRefreshTokenAsync(Arg.Any<string>(), Arg.Any<string>(), command.ClientIp, command.DeviceId, command.UserAgent, cancellationToken)
            .Returns(new RefreshTokenResult("refresh_token_plain", new RefreshToken()));

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _userRepository.Received(1).AddAsync(Arg.Is<User>(u => u.PasswordHash == "secure_hash_123"), cancellationToken);
        await _userTokenRepository.Received(1).AddAsync(Arg.Is<UserToken>(ut => ut.Purpose == "EmailVerification"), cancellationToken);
        await _passwordHistoryRepository.Received(1).AddAsync(Arg.Is<PasswordHistory>(ph => ph.PasswordHash == "secure_hash_123" && ph.Algorithm == "BCrypt"), cancellationToken);
        await _emailOutboxRepository.Received(1).AddAsync(Arg.Is<EmailOutbox>(eo => eo.TemplateName == "VerificationEmail"), cancellationToken);
        await _auditLogRepository.Received(1).AddAsync(Arg.Is<AuditLog>(al => al.Action == "UserRegistered" && al.Result == "Success"), cancellationToken);
    }


    [Fact]
    public async Task Handle_ShouldSetUserStatusToPendingVerification()
    {
        // Arrange
        var command = new RegisterCommand("statususer", "status@example.com", "Password123!", "127.0.0.1", "dev", "agent");
        var cancellationToken = CancellationToken.None;

        _userRepository.ExistsEmailAsync(command.Email, cancellationToken).Returns(false);
        _userRepository.ExistsUsernameAsync(command.Username, cancellationToken).Returns(false);
        _passwordHasher.HashPassword(Arg.Any<string>()).Returns("hash");
        _jwtGenerator.GenerateToken(Arg.Any<User>(), Arg.Any<List<string>>()).Returns("token");
        _refreshTokenService.GenerateRefreshTokenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), cancellationToken)
            .Returns(new RefreshTokenResult("plain", new RefreshToken()));

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _userRepository.Received(1).AddAsync(Arg.Is<User>(u => u.Status == Domain.Enums.UserStatus.PendingVerification), cancellationToken);
    }
}


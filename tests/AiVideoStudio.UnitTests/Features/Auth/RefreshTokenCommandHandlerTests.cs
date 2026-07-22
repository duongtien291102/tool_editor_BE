using AiVideoStudio.Application.Events;
using AiVideoStudio.Application.Features.Auth.Commands;
using AiVideoStudio.Application.Features.Auth.Handlers;
using AiVideoStudio.Application.Interfaces.Auth;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Events.Auth;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Logging;
using FluentAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AiVideoStudio.UnitTests.Features.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenService _refreshTokenService = Substitute.For<IRefreshTokenService>();
    private readonly IJwtTokenGenerator _jwtGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IPermissionResolver _permissionResolver = Substitute.For<IPermissionResolver>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IAppLogger<RefreshTokenCommandHandler> _logger = Substitute.For<IAppLogger<RefreshTokenCommandHandler>>();

    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _handler = new RefreshTokenCommandHandler(
            _refreshTokenRepository,
            _userRepository,
            _refreshTokenService,
            _jwtGenerator,
            _permissionResolver,
            _eventBus,
            _logger);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenRefreshTokenIsValidAndRotated()
    {
        // Arrange
        var command = new RefreshTokenCommand("valid_plain_token", "device_1", "agent_1", "127.0.0.1");
        var cancellationToken = CancellationToken.None;

        var tokenHash = "hashed_plain_token";
        _refreshTokenService.HashToken(command.Token).Returns(tokenHash);

        var activeUser = new User
        {
            Username = "activeuser",
            Status = UserStatus.Active
        };

        var existingToken = new RefreshToken
        {
            UserId = activeUser.Id,
            TokenHash = tokenHash,
            FamilyId = "family_abc",
            IsUsed = false,
            IsRevoked = false,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };
        _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken).Returns(existingToken);

        _userRepository.GetByIdAsync(existingToken.UserId, cancellationToken).Returns(activeUser);

        var roles = new List<string> { "User" };
        _permissionResolver.GetRolesForUserAsync(activeUser.Id, cancellationToken).Returns(roles);
        _jwtGenerator.GenerateToken(activeUser, roles).Returns("new_access_token");

        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = activeUser.Id,
            TokenHash = "new_token_hash",
            FamilyId = existingToken.FamilyId
        };
        _refreshTokenService.GenerateRefreshTokenAsync(activeUser.Id, existingToken.FamilyId, command.ClientIp, command.DeviceId, command.UserAgent, cancellationToken)
            .Returns(new RefreshTokenResult("new_plain_refresh_token", newRefreshTokenEntity));

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("new_access_token");
        result.Value.RefreshToken.Should().Be("new_plain_refresh_token");

        existingToken.IsUsed.Should().BeTrue();
        existingToken.ReplacedByTokenHash.Should().Be("new_token_hash");

        await _refreshTokenRepository.Received(2).UpdateAsync(existingToken, cancellationToken);
        await _eventBus.Received(1).PublishAsync<RefreshTokenRotatedEvent>(Arg.Is<RefreshTokenRotatedEvent>(e => e.UserId == activeUser.Id && e.OldFamilyId == existingToken.FamilyId), cancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenTokenNotFound()
    {
        // Arrange
        var command = new RefreshTokenCommand("non_existent_token", "device_1", "agent_1", "127.0.0.1");
        _refreshTokenService.HashToken(command.Token).Returns("unknown_hash");
        _refreshTokenRepository.GetByTokenHashAsync("unknown_hash", Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(AuthErrors.InvalidRefreshToken);
    }

    [Fact]
    public async Task Handle_ShouldDetectReplayAttack_AndRevokeFamily_WhenTokenIsAlreadyUsed()
    {
        // Arrange
        var command = new RefreshTokenCommand("replayed_token", "device_1", "agent_1", "192.168.1.1");
        var cancellationToken = CancellationToken.None;

        var tokenHash = "hashed_replayed_token";
        _refreshTokenService.HashToken(command.Token).Returns(tokenHash);

        var usedToken = new RefreshToken
        {
            UserId = "user_compromised",
            TokenHash = tokenHash,
            FamilyId = "family_stolen",
            IsUsed = true,
            IsRevoked = false,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };
        _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken).Returns(usedToken);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(AuthErrors.InvalidRefreshToken);

        await _refreshTokenRepository.Received(1).RevokeFamilyAsync(usedToken.FamilyId, "Replay Attack Detected", command.ClientIp, cancellationToken);
        await _eventBus.Received(1).PublishAsync<RefreshTokenCompromisedEvent>(Arg.Is<RefreshTokenCompromisedEvent>(e => e.UserId == usedToken.UserId && e.FamilyId == usedToken.FamilyId && e.SuspectedIp == command.ClientIp), cancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenTokenIsRevokedOrExpired()
    {
        // Arrange
        var command = new RefreshTokenCommand("expired_token", "device_1", "agent_1", "127.0.0.1");
        var tokenHash = "expired_token_hash";
        _refreshTokenService.HashToken(command.Token).Returns(tokenHash);

        var expiredToken = new RefreshToken
        {
            UserId = "user_1",
            TokenHash = tokenHash,
            FamilyId = "family_1",
            IsUsed = false,
            IsRevoked = false,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5) // Expired
        };
        _refreshTokenRepository.GetByTokenHashAsync(tokenHash, Arg.Any<CancellationToken>()).Returns(expiredToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(AuthErrors.InvalidRefreshToken);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotActive()
    {
        // Arrange
        var command = new RefreshTokenCommand("valid_token", "device_1", "agent_1", "127.0.0.1");
        var cancellationToken = CancellationToken.None;

        var tokenHash = "valid_token_hash";
        _refreshTokenService.HashToken(command.Token).Returns(tokenHash);

        var disabledUser = new User
        {
            Status = UserStatus.Disabled
        };

        var tokenEntity = new RefreshToken
        {
            UserId = disabledUser.Id,
            TokenHash = tokenHash,
            FamilyId = "family_1",
            IsUsed = false,
            IsRevoked = false,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        };
        _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken).Returns(tokenEntity);
        _userRepository.GetByIdAsync(tokenEntity.UserId, cancellationToken).Returns(disabledUser);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(AuthErrors.UserNotActive);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenTokenIsRevoked()
    {
        // Arrange
        var command = new RefreshTokenCommand("revoked_token", "device_1", "agent_1", "127.0.0.1");
        var tokenHash = "revoked_token_hash";
        _refreshTokenService.HashToken(command.Token).Returns(tokenHash);

        var revokedToken = new RefreshToken
        {
            UserId = "user_1",
            TokenHash = tokenHash,
            FamilyId = "family_1",
            IsUsed = false,
            IsRevoked = true, // Explicitly Revoked
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        };
        _refreshTokenRepository.GetByTokenHashAsync(tokenHash, Arg.Any<CancellationToken>()).Returns(revokedToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(AuthErrors.InvalidRefreshToken);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserDoesNotExist()
    {
        // Arrange
        var command = new RefreshTokenCommand("valid_token", "device_1", "agent_1", "127.0.0.1");
        var cancellationToken = CancellationToken.None;

        var tokenHash = "valid_token_hash";
        _refreshTokenService.HashToken(command.Token).Returns(tokenHash);

        var tokenEntity = new RefreshToken
        {
            UserId = "non_existent_user",
            TokenHash = tokenHash,
            FamilyId = "family_1",
            IsUsed = false,
            IsRevoked = false,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        };
        _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken).Returns(tokenEntity);
        _userRepository.GetByIdAsync(tokenEntity.UserId, cancellationToken).Returns((User?)null);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(AuthErrors.UserNotActive);
    }
}


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

public class ChangePasswordCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHistoryRepository _passwordHistoryRepository = Substitute.For<IPasswordHistoryRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly ITransactionManager _transactionManager = Substitute.For<ITransactionManager>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();

    private readonly ChangePasswordCommandHandler _handler;

    public ChangePasswordCommandHandlerTests()
    {
        _handler = new ChangePasswordCommandHandler(
            _userRepository,
            _passwordHistoryRepository,
            _refreshTokenRepository,
            _transactionManager,
            _passwordHasher);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenCurrentPasswordIsValidAndNewPasswordIsUnique()
    {
        // Arrange
        var user = new User
        {
            PasswordHash = "old_hash"
        };
        var command = new ChangePasswordCommand(user.Id, "OldPass123!", "NewPass123!", "127.0.0.1");
        var cancellationToken = CancellationToken.None;

        _userRepository.GetByIdAsync(command.UserId, cancellationToken).Returns(user);
        _passwordHasher.VerifyPassword(command.CurrentPassword, user.PasswordHash).Returns(true);

        _passwordHistoryRepository.GetRecentByUserIdAsync(user.Id, 5, cancellationToken)
            .Returns(new List<PasswordHistory>
            {
                new PasswordHistory { UserId = user.Id, PasswordHash = "hist_hash_1" }
            });
        _passwordHasher.VerifyPassword(command.NewPassword, "hist_hash_1").Returns(false);
        _passwordHasher.HashPassword(command.NewPassword).Returns("new_hash");

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        user.PasswordHash.Should().Be("new_hash");

        await _transactionManager.Received(1).BeginTransactionAsync(cancellationToken);
        await _userRepository.Received(1).UpdateAsync(user, cancellationToken);
        await _passwordHistoryRepository.Received(1).AddAsync(Arg.Is<PasswordHistory>(ph => ph.UserId == user.Id && ph.PasswordHash == "new_hash"), cancellationToken);
        await _refreshTokenRepository.Received(1).RevokeAllForUserAsync(user.Id, "PasswordChanged", command.ClientIp, cancellationToken);
        await _transactionManager.Received(1).CommitTransactionAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserNotFound()
    {
        // Arrange
        var command = new ChangePasswordCommand("unknown_user", "OldPass123!", "NewPass123!", "127.0.0.1");
        _userRepository.GetByIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCurrentPasswordIsWrong()
    {
        // Arrange
        var user = new User { PasswordHash = "old_hash" };
        var command = new ChangePasswordCommand(user.Id, "WrongOldPass", "NewPass123!", "127.0.0.1");

        _userRepository.GetByIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyPassword(command.CurrentPassword, user.PasswordHash).Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(AuthErrors.InvalidCredentials);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenNewPasswordMatchesPasswordHistory()
    {
        // Arrange
        var user = new User { PasswordHash = "old_hash" };
        var command = new ChangePasswordCommand(user.Id, "OldPass123!", "ReusedPass123!", "127.0.0.1");
        var cancellationToken = CancellationToken.None;

        _userRepository.GetByIdAsync(command.UserId, cancellationToken).Returns(user);
        _passwordHasher.VerifyPassword(command.CurrentPassword, user.PasswordHash).Returns(true);

        _passwordHistoryRepository.GetRecentByUserIdAsync(user.Id, 5, cancellationToken)
            .Returns(new List<PasswordHistory>
            {
                new PasswordHistory { UserId = user.Id, PasswordHash = "reused_hash" }
            });
        _passwordHasher.VerifyPassword(command.NewPassword, "reused_hash").Returns(true);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(AuthErrors.PasswordReuse);
    }

    [Fact]
    public async Task Handle_ShouldRollbackTransaction_WhenUpdateFails()
    {
        // Arrange
        var user = new User { PasswordHash = "old_hash" };
        var command = new ChangePasswordCommand(user.Id, "OldPass123!", "NewPass123!", "127.0.0.1");
        var cancellationToken = CancellationToken.None;

        _userRepository.GetByIdAsync(command.UserId, cancellationToken).Returns(user);
        _passwordHasher.VerifyPassword(command.CurrentPassword, user.PasswordHash).Returns(true);
        _passwordHistoryRepository.GetRecentByUserIdAsync(user.Id, 5, cancellationToken).Returns(new List<PasswordHistory>());
        _passwordHasher.HashPassword(command.NewPassword).Returns("new_hash");

        _userRepository.UpdateAsync(user, cancellationToken).ThrowsAsync(new InvalidOperationException("DB Update Exception"));

        // Act
        Func<Task> act = async () => await _handler.Handle(command, cancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DB Update Exception");
        await _transactionManager.Received(1).AbortTransactionAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldRevokeAllRefreshTokens_WhenPasswordChanged()
    {
        // Arrange
        var user = new User { PasswordHash = "hash_old" };
        var command = new ChangePasswordCommand(user.Id, "Old123!", "New123!", "10.0.0.1");
        var cancellationToken = CancellationToken.None;

        _userRepository.GetByIdAsync(user.Id, cancellationToken).Returns(user);
        _passwordHasher.VerifyPassword(command.CurrentPassword, user.PasswordHash).Returns(true);
        _passwordHistoryRepository.GetRecentByUserIdAsync(user.Id, 5, cancellationToken).Returns(new List<PasswordHistory>());
        _passwordHasher.HashPassword(command.NewPassword).Returns("hash_new");

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _refreshTokenRepository.Received(1).RevokeAllForUserAsync(user.Id, "PasswordChanged", command.ClientIp, cancellationToken);
    }
}


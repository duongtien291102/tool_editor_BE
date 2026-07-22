using AiVideoStudio.Application.Events;
using AiVideoStudio.Application.Features.Auth.Commands;
using AiVideoStudio.Application.Features.Auth.Handlers;
using AiVideoStudio.Application.Interfaces.Auth;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Events.Auth;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Logging;
using AiVideoStudio.Shared.Responses;
using FluentAssertions;
using NSubstitute;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AiVideoStudio.UnitTests.Features.Auth;

public class LoginCommandHandlerTests
{
    private readonly IAuthenticationService _authService = Substitute.For<IAuthenticationService>();
    private readonly IJwtTokenGenerator _jwtGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IRefreshTokenService _refreshTokenService = Substitute.For<IRefreshTokenService>();
    private readonly IPermissionResolver _permissionResolver = Substitute.For<IPermissionResolver>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IAppLogger<LoginCommandHandler> _logger = Substitute.For<IAppLogger<LoginCommandHandler>>();

    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(
            _authService,
            _jwtGenerator,
            _refreshTokenService,
            _permissionResolver,
            _eventBus,
            _logger);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenCredentialsAreValid()
    {
        // Arrange
        var command = new LoginCommand("testuser", "Password123!", "device1", "agent", "127.0.0.1");
        var cancellationToken = CancellationToken.None;

        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com"
        };
        var roles = new List<string> { "Admin", "User" };

        _authService.VerifyCredentialsAsync(command.Username, command.Password, cancellationToken)
            .Returns(Result<User>.Success(user));
        _permissionResolver.GetRolesForUserAsync(user.Id, cancellationToken)
            .Returns(roles);
        _jwtGenerator.GenerateToken(user, roles).Returns("jwt_access_token");

        var refreshEntity = new RefreshToken { UserId = user.Id, FamilyId = "family_1" };
        _refreshTokenService.GenerateRefreshTokenAsync(user.Id, string.Empty, command.ClientIp, command.DeviceId, command.UserAgent, cancellationToken)
            .Returns(new RefreshTokenResult("plain_refresh_token", refreshEntity));

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("jwt_access_token");
        result.Value.RefreshToken.Should().Be("plain_refresh_token");

        await _eventBus.Received(1).PublishAsync<UserLoggedInEvent>(Arg.Is<UserLoggedInEvent>(e => e.UserId == user.Id && e.IpAddress == command.ClientIp), cancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCredentialsAreInvalid()
    {
        // Arrange
        var command = new LoginCommand("testuser", "WrongPassword", "device1", "agent", "127.0.0.1");
        var cancellationToken = CancellationToken.None;

        _authService.VerifyCredentialsAsync(command.Username, command.Password, cancellationToken)
            .Returns(Result<User>.Failure(AuthErrors.InvalidCredentials));

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(AuthErrors.InvalidCredentials);

        _jwtGenerator.DidNotReceive().GenerateToken(Arg.Any<User>(), Arg.Any<List<string>>());
        await _eventBus.DidNotReceive().PublishAsync<UserLoggedInEvent>(Arg.Any<UserLoggedInEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotActive()
    {
        // Arrange
        var command = new LoginCommand("inactiveuser", "Password123!", "device1", "agent", "127.0.0.1");
        var cancellationToken = CancellationToken.None;

        _authService.VerifyCredentialsAsync(command.Username, command.Password, cancellationToken)
            .Returns(Result<User>.Failure(AuthErrors.UserNotActive));

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(AuthErrors.UserNotActive);

        _jwtGenerator.DidNotReceive().GenerateToken(Arg.Any<User>(), Arg.Any<List<string>>());
        await _eventBus.DidNotReceive().PublishAsync<UserLoggedInEvent>(Arg.Any<UserLoggedInEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPassResolvedRolesToJwtGenerator()
    {
        // Arrange
        var command = new LoginCommand("testuser", "Password123!", "device1", "agent", "127.0.0.1");
        var cancellationToken = CancellationToken.None;
        var user = new User { Username = "testuser" };
        var roles = new List<string> { "ContentCreator" };

        _authService.VerifyCredentialsAsync(command.Username, command.Password, cancellationToken)
            .Returns(Result<User>.Success(user));
        _permissionResolver.GetRolesForUserAsync(user.Id, cancellationToken).Returns(roles);
        _jwtGenerator.GenerateToken(user, roles).Returns("jwt_token_with_roles");

        var refreshEntity = new RefreshToken { UserId = user.Id };
        _refreshTokenService.GenerateRefreshTokenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), cancellationToken)
            .Returns(new RefreshTokenResult("refresh_token", refreshEntity));

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _jwtGenerator.Received(1).GenerateToken(user, roles);
    }

    [Fact]
    public async Task Handle_ShouldReturnAuthResponseWithRefreshToken_WhenLoginSucceeds()
    {
        // Arrange
        var command = new LoginCommand("validuser", "Pass123!", "device_x", "agent_y", "10.0.0.1");
        var cancellationToken = CancellationToken.None;
        var user = new User { Username = "validuser" };

        _authService.VerifyCredentialsAsync(command.Username, command.Password, cancellationToken)
            .Returns(Result<User>.Success(user));
        _permissionResolver.GetRolesForUserAsync(user.Id, cancellationToken).Returns(new List<string>());
        _jwtGenerator.GenerateToken(user, Arg.Any<List<string>>()).Returns("access_123");
        _refreshTokenService.GenerateRefreshTokenAsync(user.Id, string.Empty, command.ClientIp, command.DeviceId, command.UserAgent, cancellationToken)
            .Returns(new RefreshTokenResult("refresh_123", new RefreshToken()));

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("access_123");
        result.Value.RefreshToken.Should().Be("refresh_123");
        result.Value.ExpiresIn.Should().Be(3600);
    }
}


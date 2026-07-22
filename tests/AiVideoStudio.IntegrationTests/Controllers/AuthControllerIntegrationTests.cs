using AiVideoStudio.Application.Interfaces.Auth;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Shared.ApiContracts.V1.Auth.Requests;
using AiVideoStudio.Shared.ApiContracts.V1.Auth.Responses;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using FluentAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AiVideoStudio.IntegrationTests.Controllers;

public class AuthControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsOkAndSetsRefreshTokenCookie()
    {
        // Arrange
        var request = new RegisterRequest("newuser", "newuser@example.com", "P@ssword123!", "device1");

        _factory.UserRepository.ExistsEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns(false);
        _factory.UserRepository.ExistsUsernameAsync(request.Username, Arg.Any<CancellationToken>()).Returns(false);
        _factory.PasswordHasher.HashPassword(Arg.Any<string>()).Returns("hashed_pwd");
        _factory.JwtGenerator.GenerateToken(Arg.Any<User>(), Arg.Any<List<string>>()).Returns("valid_jwt_access_token");
        _factory.RefreshTokenService.GenerateRefreshTokenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new RefreshTokenResult("plain_refresh_token_123", new RefreshToken()));

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.AccessToken.Should().Be("valid_jwt_access_token");

        response.Headers.TryGetValues("Set-Cookie", out var cookieHeader).Should().BeTrue();
        cookieHeader.Should().Contain(c => c.Contains("refreshToken=plain_refresh_token_123"));
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ReturnsConflict()
    {
        // Arrange
        var request = new RegisterRequest("user1", "existing@example.com", "P@ssword123!", "device1");
        _factory.UserRepository.ExistsEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.ErrorCode.Should().Be("USER.EMAIL_EXISTS");
    }

    [Fact]
    public async Task Register_WhenUsernameAlreadyExists_ReturnsConflict()
    {
        // Arrange
        var request = new RegisterRequest("existinguser", "newuser@example.com", "P@ssword123!", "device1");
        _factory.UserRepository.ExistsEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns(false);
        _factory.UserRepository.ExistsUsernameAsync(request.Username, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.ErrorCode.Should().Be("USER.USERNAME_EXISTS");
    }


    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndSetsRefreshTokenCookie()
    {
        // Arrange
        var request = new LoginRequest("validuser", "P@ssword123!", "device1");
        var user = new User { Username = "validuser", Email = "valid@example.com" };

        _factory.AuthenticationService.VerifyCredentialsAsync(request.Username, request.Password, Arg.Any<CancellationToken>())
            .Returns(Result<User>.Success(user));
        _factory.PermissionResolver.GetRolesForUserAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "User" });
        _factory.JwtGenerator.GenerateToken(user, Arg.Any<List<string>>()).Returns("jwt_login_token");
        _factory.RefreshTokenService.GenerateRefreshTokenAsync(user.Id, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new RefreshTokenResult("refresh_login_cookie_val", new RefreshToken()));

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.AccessToken.Should().Be("jwt_login_token");

        response.Headers.TryGetValues("Set-Cookie", out var cookieHeader).Should().BeTrue();
        cookieHeader.Should().Contain(c => c.Contains("refreshToken=refresh_login_cookie_val"));
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest("wronguser", "WrongPass123!", "device1");
        _factory.AuthenticationService.VerifyCredentialsAsync(request.Username, request.Password, Arg.Any<CancellationToken>())
            .Returns(Result<User>.Failure(AuthErrors.InvalidCredentials));

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.ErrorCode.Should().Be("AUTH.INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Login_WithMissingCredentials_ReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest("", "", "device_1");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ReturnsUnauthorized()
    {
        // Arrange
        var request = new RefreshTokenRequest("device_1");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.ErrorCode.Should().Be("AUTH.MISSING_TOKEN");
    }

    [Fact]
    public async Task Refresh_WithValidCookie_ReturnsOkAndRotatesCookie()
    {
        // Arrange
        var request = new RefreshTokenRequest("device_1");
        var activeUser = new User { Username = "activeuser", Status = Domain.Enums.UserStatus.Active };
        var tokenEntity = new RefreshToken
        {
            UserId = activeUser.Id,
            TokenHash = "hash_valid",
            FamilyId = "family_1",
            IsUsed = false,
            IsRevoked = false,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        _factory.RefreshTokenService.HashToken("existing_refresh_token").Returns("hash_valid");
        _factory.RefreshTokenRepository.GetByTokenHashAsync("hash_valid", Arg.Any<CancellationToken>()).Returns(tokenEntity);
        _factory.UserRepository.GetByIdAsync(activeUser.Id, Arg.Any<CancellationToken>()).Returns(activeUser);
        _factory.PermissionResolver.GetRolesForUserAsync(activeUser.Id, Arg.Any<CancellationToken>()).Returns(new List<string>());
        _factory.JwtGenerator.GenerateToken(activeUser, Arg.Any<List<string>>()).Returns("new_refreshed_access_token");
        _factory.RefreshTokenService.GenerateRefreshTokenAsync(activeUser.Id, "family_1", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new RefreshTokenResult("new_plain_refresh_cookie", new RefreshToken { TokenHash = "new_hash" }));

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("Cookie", "refreshToken=existing_refresh_token");

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.AccessToken.Should().Be("new_refreshed_access_token");

        response.Headers.TryGetValues("Set-Cookie", out var cookieHeader).Should().BeTrue();
        cookieHeader.Should().Contain(c => c.Contains("refreshToken=new_plain_refresh_cookie"));
    }
}

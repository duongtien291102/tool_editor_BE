using AiVideoStudio.Application.Configuration;
using AiVideoStudio.Application.Features.Auth.Commands;
using AiVideoStudio.Application.Features.Auth.Validators;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AiVideoStudio.UnitTests.Features.Auth;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator;

    public LoginCommandValidatorTests()
    {
        var options = Options.Create(new PasswordPolicyOptions
        {
            MinLength = 8,
            RequireUppercase = true,
            RequireLowercase = true,
            RequireDigit = true,
            RequireSpecialCharacter = true
        });

        _validator = new LoginCommandValidator(options);
    }

    [Fact]
    public void Validate_ShouldBeValid_WhenCommandFulfillsAllRules()
    {
        // Arrange
        var command = new LoginCommand("validUser", "P@ssword123", "device", "agent", "127.0.0.1");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "P@ssword123", "Username is required.")]
    [InlineData("validUser", "", "Password is required.")]
    [InlineData("validUser", "Short1!", "Password must be at least 8 characters.")]
    [InlineData("validUser", "lowercase1!", "Password must contain at least one uppercase letter.")]
    [InlineData("validUser", "UPPERCASE1!", "Password must contain at least one lowercase letter.")]
    [InlineData("validUser", "P@sswordNoDigit", "Password must contain at least one number.")]
    [InlineData("validUser", "Password123NoSpecial", "Password must contain at least one special character.")]
    public void Validate_ShouldFail_WhenRulesAreViolated(string username, string password, string expectedErrorMessage)
    {
        // Arrange
        var command = new LoginCommand(username, password, "device", "agent", "127.0.0.1");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == expectedErrorMessage);
    }

    [Fact]
    public void Validate_ShouldAllowPasswordWithoutSpecialChar_WhenSpecialCharNotRequiredInOptions()
    {
        // Arrange
        var relaxedOptions = Options.Create(new PasswordPolicyOptions
        {
            MinLength = 6,
            RequireUppercase = true,
            RequireLowercase = true,
            RequireDigit = true,
            RequireSpecialCharacter = false
        });
        var relaxedValidator = new LoginCommandValidator(relaxedOptions);

        var command = new LoginCommand("validUser", "Password123", "device", "agent", "127.0.0.1");

        // Act
        var result = relaxedValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}


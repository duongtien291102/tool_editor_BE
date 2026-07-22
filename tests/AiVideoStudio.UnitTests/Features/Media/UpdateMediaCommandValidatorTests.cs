using AiVideoStudio.Application.Features.Media.Commands;
using AiVideoStudio.Application.Features.Media.Validators;
using FluentAssertions;
using Xunit;

namespace AiVideoStudio.UnitTests.Features.Media;

public class UpdateMediaCommandValidatorTests
{
    private readonly UpdateMediaCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenIdIsProvided()
    {
        // Arrange
        var command = new UpdateMediaCommand("media_123", "new_name.jpg", 1920, 1080, 15.5);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenIdIsEmpty()
    {
        // Arrange
        var command = new UpdateMediaCommand("");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Media ID is required.");
    }
}

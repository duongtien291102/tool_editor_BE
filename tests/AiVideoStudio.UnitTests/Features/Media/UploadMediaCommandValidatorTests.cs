using AiVideoStudio.Application.Features.Media.Commands;
using AiVideoStudio.Application.Features.Media.Validators;
using FluentAssertions;
using System.IO;
using System.Text;
using Xunit;

namespace AiVideoStudio.UnitTests.Features.Media;

public class UploadMediaCommandValidatorTests
{
    private readonly UploadMediaCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenAllFieldsAreProvided()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
        var command = new UploadMediaCommand("proj_123", "video.mp4", "video/mp4", 1024, stream);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenProjectIdIsEmpty()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
        var command = new UploadMediaCommand("", "video.mp4", "video/mp4", 1024, stream);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "ProjectId is required.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenFileSizeIsZeroOrNegative()
    {
        // Arrange
        using var stream = new MemoryStream();
        var command = new UploadMediaCommand("proj_1", "video.mp4", "video/mp4", 0, stream);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "FileSize must be greater than zero.");
    }
}

using AiVideoStudio.Application.Features.Projects.Commands;
using AiVideoStudio.Application.Features.Projects.Validators;
using FluentAssertions;
using Xunit;

namespace AiVideoStudio.UnitTests.Features.Projects;

public class UpdateProjectCommandValidatorTests
{
    private readonly UpdateProjectCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenIdAndNameAreProvided()
    {
        // Arrange
        var command = new UpdateProjectCommand("proj_123", "Valid Project Name", "Description");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenIdIsEmpty()
    {
        // Arrange
        var command = new UpdateProjectCommand("", "Valid Name");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Project ID is required.");
    }
}

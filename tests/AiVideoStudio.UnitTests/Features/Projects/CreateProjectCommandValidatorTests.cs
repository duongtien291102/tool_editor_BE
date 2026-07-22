using AiVideoStudio.Application.Features.Projects.Commands;
using AiVideoStudio.Application.Features.Projects.Validators;
using FluentAssertions;
using Xunit;

namespace AiVideoStudio.UnitTests.Features.Projects;

public class CreateProjectCommandValidatorTests
{
    private readonly CreateProjectCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenNameIsProvided()
    {
        // Arrange
        var command = new CreateProjectCommand("Valid Project Name", "Description", "http://thumb.jpg");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Project name is required.")]
    [InlineData("   ", "Project name is required.")]
    public void Validate_ShouldFail_WhenNameIsEmpty(string name, string expectedErrorMessage)
    {
        // Arrange
        var command = new CreateProjectCommand(name);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == expectedErrorMessage);
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameExceeds200Chars()
    {
        // Arrange
        var longName = new string('A', 201);
        var command = new CreateProjectCommand(longName);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Project name must not exceed 200 characters.");
    }
}

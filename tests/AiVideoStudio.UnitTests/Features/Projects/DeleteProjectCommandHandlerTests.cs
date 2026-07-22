using AiVideoStudio.Application.Events;
using AiVideoStudio.Application.Features.Projects.Commands;
using AiVideoStudio.Application.Features.Projects.Handlers;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Events.Projects;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using FluentAssertions;
using NSubstitute;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AiVideoStudio.UnitTests.Features.Projects;

public class DeleteProjectCommandHandlerTests
{
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private readonly DeleteProjectCommandHandler _handler;

    public DeleteProjectCommandHandlerTests()
    {
        _handler = new DeleteProjectCommandHandler(
            _projectRepository,
            _currentUser,
            _eventBus);
    }

    [Fact]
    public async Task Handle_ShouldSoftDeleteProject_WhenOwnerDeletesProject()
    {
        // Arrange
        var ownerId = "owner_123";
        var existingProject = Project.Create("Project to Delete", ownerId);
        var command = new DeleteProjectCommand(existingProject.Id);
        var cancellationToken = CancellationToken.None;

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(ownerId);
        _currentUser.Roles.Returns(new List<string> { "User" });

        _projectRepository.GetByIdAsync(existingProject.Id, cancellationToken).Returns(existingProject);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        existingProject.IsDeleted.Should().BeTrue();
        existingProject.DeletedBy.Should().Be(ownerId);

        await _projectRepository.Received(1).UpdateAsync(existingProject, cancellationToken);
        await _eventBus.Received(1).PublishAsync(Arg.Is<ProjectDeletedEvent>(e => e.ProjectId == existingProject.Id && e.DeletedBy == ownerId), cancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldSoftDeleteProject_WhenAdminDeletesProject()
    {
        // Arrange
        var ownerId = "owner_123";
        var adminId = "admin_999";
        var existingProject = Project.Create("Project to Delete", ownerId);
        var command = new DeleteProjectCommand(existingProject.Id);
        var cancellationToken = CancellationToken.None;

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(adminId);
        _currentUser.Roles.Returns(new List<string> { "Admin" });

        _projectRepository.GetByIdAsync(existingProject.Id, cancellationToken).Returns(existingProject);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingProject.IsDeleted.Should().BeTrue();
        existingProject.DeletedBy.Should().Be(adminId);

        await _projectRepository.Received(1).UpdateAsync(existingProject, cancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenStrangerTriesToDeleteProject()
    {
        // Arrange
        var ownerId = "owner_123";
        var strangerId = "stranger_456";
        var existingProject = Project.Create("Project to Delete", ownerId);
        var command = new DeleteProjectCommand(existingProject.Id);

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(strangerId);
        _currentUser.Roles.Returns(new List<string> { "User" });

        _projectRepository.GetByIdAsync(existingProject.Id, Arg.Any<CancellationToken>()).Returns(existingProject);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.UnauthorizedAccess);
        existingProject.IsDeleted.Should().BeFalse();
        await _projectRepository.DidNotReceive().UpdateAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProjectNotFound()
    {
        // Arrange
        var command = new DeleteProjectCommand("invalid_id");
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns("user_1");

        _projectRepository.GetByIdAsync("invalid_id", Arg.Any<CancellationToken>()).Returns((Project?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.NotFound);
    }
}

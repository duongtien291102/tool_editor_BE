using AiVideoStudio.Application.Events;
using AiVideoStudio.Application.Features.Projects.Commands;
using AiVideoStudio.Application.Features.Projects.Handlers;
using AiVideoStudio.Application.Features.Projects.Mappings;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Events.Projects;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AiVideoStudio.UnitTests.Features.Projects;

public class UpdateProjectCommandHandlerTests
{
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IMapper _mapper;
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private readonly UpdateProjectCommandHandler _handler;

    public UpdateProjectCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ProjectProfile>());
        _mapper = config.CreateMapper();

        _handler = new UpdateProjectCommandHandler(
            _projectRepository,
            _currentUser,
            _mapper,
            _eventBus);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenOwnerUpdatesProject()
    {
        // Arrange
        var ownerId = "owner_123";
        var existingProject = Project.Create("Old Name", ownerId);
        var command = new UpdateProjectCommand(existingProject.Id, "Updated Name", "Updated Desc", "http://thumb2.jpg", ProjectStatus.Active);
        var cancellationToken = CancellationToken.None;

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(ownerId);
        _currentUser.Roles.Returns(new List<string> { "User" });

        _projectRepository.GetByIdAsync(existingProject.Id, cancellationToken).Returns(existingProject);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Name");
        result.Value.Status.Should().Be(ProjectStatus.Active);

        await _projectRepository.Received(1).UpdateAsync(existingProject, cancellationToken);
        await _eventBus.Received(1).PublishAsync(Arg.Is<ProjectUpdatedEvent>(e => e.ProjectId == existingProject.Id), cancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenAdminUpdatesProjectOfOtherUser()
    {
        // Arrange
        var ownerId = "owner_123";
        var adminId = "admin_999";
        var existingProject = Project.Create("Old Name", ownerId);
        var command = new UpdateProjectCommand(existingProject.Id, "Admin Overridden Name", null, null, ProjectStatus.Completed);
        var cancellationToken = CancellationToken.None;

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(adminId);
        _currentUser.Roles.Returns(new List<string> { "Admin" });

        _projectRepository.GetByIdAsync(existingProject.Id, cancellationToken).Returns(existingProject);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Admin Overridden Name");
        await _projectRepository.Received(1).UpdateAsync(existingProject, cancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotOwnerNorAdmin()
    {
        // Arrange
        var ownerId = "owner_123";
        var strangerId = "stranger_456";
        var existingProject = Project.Create("Old Name", ownerId);
        var command = new UpdateProjectCommand(existingProject.Id, "Hacked Name");

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(strangerId);
        _currentUser.Roles.Returns(new List<string> { "User" });

        _projectRepository.GetByIdAsync(existingProject.Id, Arg.Any<CancellationToken>()).Returns(existingProject);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.UnauthorizedAccess);
        await _projectRepository.DidNotReceive().UpdateAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProjectDoesNotExist()
    {
        // Arrange
        var command = new UpdateProjectCommand("non_existent_id", "Name");
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns("user_1");

        _projectRepository.GetByIdAsync("non_existent_id", Arg.Any<CancellationToken>()).Returns((Project?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.NotFound);
    }
}

using AiVideoStudio.Application.Features.Projects.DTOs;
using AiVideoStudio.Application.Features.Projects.Handlers;
using AiVideoStudio.Application.Features.Projects.Mappings;
using AiVideoStudio.Application.Features.Projects.Queries;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Domain.Entities;
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

public class GetProjectByIdQueryHandlerTests
{
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IMapper _mapper;

    private readonly GetProjectByIdQueryHandler _handler;

    public GetProjectByIdQueryHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ProjectProfile>());
        _mapper = config.CreateMapper();

        _handler = new GetProjectByIdQueryHandler(
            _projectRepository,
            _currentUser,
            _mapper);
    }

    [Fact]
    public async Task Handle_ShouldReturnProject_WhenOwnerRequestsProject()
    {
        // Arrange
        var ownerId = "owner_123";
        var existingProject = Project.Create("My Project", ownerId);
        var query = new GetProjectByIdQuery(existingProject.Id);
        var cancellationToken = CancellationToken.None;

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(ownerId);
        _currentUser.Roles.Returns(new List<string> { "User" });

        _projectRepository.GetByIdAsync(existingProject.Id, cancellationToken).Returns(existingProject);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(existingProject.Id);
        result.Value.Name.Should().Be("My Project");
    }

    [Fact]
    public async Task Handle_ShouldReturnProject_WhenAdminRequestsProject()
    {
        // Arrange
        var ownerId = "owner_123";
        var adminId = "admin_999";
        var existingProject = Project.Create("User Project", ownerId);
        var query = new GetProjectByIdQuery(existingProject.Id);

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(adminId);
        _currentUser.Roles.Returns(new List<string> { "Admin" });

        _projectRepository.GetByIdAsync(existingProject.Id, Arg.Any<CancellationToken>()).Returns(existingProject);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.OwnerId.Should().Be(ownerId);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenNonOwnerAndNonAdminRequestsProject()
    {
        // Arrange
        var ownerId = "owner_123";
        var strangerId = "stranger_456";
        var existingProject = Project.Create("Private Project", ownerId);
        var query = new GetProjectByIdQuery(existingProject.Id);

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(strangerId);
        _currentUser.Roles.Returns(new List<string> { "User" });

        _projectRepository.GetByIdAsync(existingProject.Id, Arg.Any<CancellationToken>()).Returns(existingProject);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.UnauthorizedAccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProjectNotFound()
    {
        // Arrange
        var query = new GetProjectByIdQuery("invalid_id");
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns("user_1");

        _projectRepository.GetByIdAsync("invalid_id", Arg.Any<CancellationToken>()).Returns((Project?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.NotFound);
    }
}

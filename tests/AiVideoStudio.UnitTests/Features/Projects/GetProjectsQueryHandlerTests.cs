using AiVideoStudio.Application.Features.Projects.DTOs;
using AiVideoStudio.Application.Features.Projects.Handlers;
using AiVideoStudio.Application.Features.Projects.Mappings;
using AiVideoStudio.Application.Features.Projects.Queries;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AiVideoStudio.UnitTests.Features.Projects;

public class GetProjectsQueryHandlerTests
{
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IMapper _mapper;

    private readonly GetProjectsQueryHandler _handler;

    public GetProjectsQueryHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ProjectProfile>());
        _mapper = config.CreateMapper();

        _handler = new GetProjectsQueryHandler(
            _projectRepository,
            _currentUser,
            _mapper);
    }

    [Fact]
    public async Task Handle_ShouldScopeToUserId_WhenUserIsNotAdmin()
    {
        // Arrange
        var userId = "user_regular_123";
        var query = new GetProjectsQuery(1, 10, "demo", "name", false, ProjectStatus.Active);
        var cancellationToken = CancellationToken.None;

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);
        _currentUser.Roles.Returns(new List<string> { "User" });

        var mockProjects = new List<Project>
        {
            Project.Create("Demo Project 1", userId),
            Project.Create("Demo Project 2", userId)
        };
        var pagedResult = new PagedResult<Project>(mockProjects, 2, 1, 10);

        _projectRepository.GetPagedAsync(userId, false, 1, 10, "demo", "name", false, ProjectStatus.Active, cancellationToken)
            .Returns(pagedResult);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);

        await _projectRepository.Received(1).GetPagedAsync(userId, false, 1, 10, "demo", "name", false, ProjectStatus.Active, cancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldNotScopeOwnerId_WhenUserIsAdmin()
    {
        // Arrange
        var adminId = "admin_999";
        var query = new GetProjectsQuery(1, 20, null, "createdat", true, null);
        var cancellationToken = CancellationToken.None;

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(adminId);
        _currentUser.Roles.Returns(new List<string> { "Admin" });

        var mockProjects = new List<Project>
        {
            Project.Create("User 1 Project", "user_1"),
            Project.Create("User 2 Project", "user_2")
        };
        var pagedResult = new PagedResult<Project>(mockProjects, 2, 1, 20);

        _projectRepository.GetPagedAsync(null, true, 1, 20, null, "createdat", true, null, cancellationToken)
            .Returns(pagedResult);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);

        await _projectRepository.Received(1).GetPagedAsync(null, true, 1, 20, null, "createdat", true, null, cancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var query = new GetProjectsQuery();
        _currentUser.IsAuthenticated.Returns(false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(AuthErrors.Unauthorized);
    }
}

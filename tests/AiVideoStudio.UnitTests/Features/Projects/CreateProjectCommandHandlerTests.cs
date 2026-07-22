using AiVideoStudio.Application.Events;
using AiVideoStudio.Application.Features.Projects.Commands;
using AiVideoStudio.Application.Features.Projects.DTOs;
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
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AiVideoStudio.UnitTests.Features.Projects;

public class CreateProjectCommandHandlerTests
{
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IMapper _mapper;
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private readonly CreateProjectCommandHandler _handler;

    public CreateProjectCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ProjectProfile>());
        _mapper = config.CreateMapper();

        _handler = new CreateProjectCommandHandler(
            _projectRepository,
            _currentUser,
            _mapper,
            _eventBus);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenCommandIsValidAndUserIsAuthenticated()
    {
        // Arrange
        var command = new CreateProjectCommand("My New Project", "Description text", "http://thumb.jpg");
        var cancellationToken = CancellationToken.None;

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns("user_owner_123");

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("My New Project");
        result.Value.OwnerId.Should().Be("user_owner_123");
        result.Value.Status.Should().Be(ProjectStatus.Draft);

        await _projectRepository.Received(1).AddAsync(Arg.Is<Project>(p => p.Name == "My New Project" && p.OwnerId == "user_owner_123"), cancellationToken);
        await _eventBus.Received(1).PublishAsync(Arg.Is<ProjectCreatedEvent>(e => e.OwnerId == "user_owner_123"), cancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var command = new CreateProjectCommand("My New Project");
        _currentUser.IsAuthenticated.Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(AuthErrors.Unauthorized);
        await _projectRepository.DidNotReceive().AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenNameIsEmpty()
    {
        // Arrange
        var command = new CreateProjectCommand("");
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns("user_1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.NameRequired);
        await _projectRepository.DidNotReceive().AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
    }
}

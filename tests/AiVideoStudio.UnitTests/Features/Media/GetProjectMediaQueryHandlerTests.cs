using AiVideoStudio.Application.Features.Media.Handlers;
using AiVideoStudio.Application.Features.Media.Mappings;
using AiVideoStudio.Application.Features.Media.Queries;
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

namespace AiVideoStudio.UnitTests.Features.Media;

public class GetProjectMediaQueryHandlerTests
{
    private readonly IMediaAssetRepository _mediaAssetRepository = Substitute.For<IMediaAssetRepository>();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IMapper _mapper;

    private readonly GetProjectMediaQueryHandler _handler;

    public GetProjectMediaQueryHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MediaProfile>());
        _mapper = config.CreateMapper();

        _handler = new GetProjectMediaQueryHandler(
            _mediaAssetRepository,
            _projectRepository,
            _currentUser,
            _mapper);
    }

    [Fact]
    public async Task Handle_ShouldReturnMediaList_WhenOwnerRequestsProjectMedia()
    {
        // Arrange
        var ownerId = "owner_123";
        var project = Project.Create("Demo Project", ownerId);
        var query = new GetProjectMediaQuery(project.Id, 1, 10, "avatar", "filename", false, AssetType.Image, MediaStatus.Ready);

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(ownerId);
        _currentUser.Roles.Returns(new List<string> { "User" });

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var mediaList = new List<MediaAsset>
        {
            MediaAsset.Create(project.Id, ownerId, "avatar.jpg", "avatar.jpg", ".jpg", "image/jpeg", 1024, "path1", AssetType.Image),
            MediaAsset.Create(project.Id, ownerId, "avatar2.png", "avatar2.png", ".png", "image/png", 2048, "path2", AssetType.Image)
        };
        var pagedResult = new PagedResult<MediaAsset>(mediaList, 2, 1, 10);

        _mediaAssetRepository.GetPagedByProjectIdAsync(project.Id, 1, 10, "avatar", "filename", false, AssetType.Image, MediaStatus.Ready, Arg.Any<CancellationToken>())
            .Returns(pagedResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);

        await _mediaAssetRepository.Received(1).GetPagedByProjectIdAsync(project.Id, 1, 10, "avatar", "filename", false, AssetType.Image, MediaStatus.Ready, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProjectNotFound()
    {
        // Arrange
        var query = new GetProjectMediaQuery("missing_proj");

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns("user_1");

        _projectRepository.GetByIdAsync("missing_proj", Arg.Any<CancellationToken>()).Returns((Project?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(MediaErrors.ProjectNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenStrangerRequestsProjectMedia()
    {
        // Arrange
        var ownerId = "owner_123";
        var strangerId = "stranger_456";
        var project = Project.Create("Demo Project", ownerId);
        var query = new GetProjectMediaQuery(project.Id);

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(strangerId);
        _currentUser.Roles.Returns(new List<string> { "User" });

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(MediaErrors.UnauthorizedAccess);
    }
}

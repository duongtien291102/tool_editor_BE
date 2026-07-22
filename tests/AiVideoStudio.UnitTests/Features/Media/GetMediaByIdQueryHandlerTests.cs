using AiVideoStudio.Application.Features.Media.Handlers;
using AiVideoStudio.Application.Features.Media.Mappings;
using AiVideoStudio.Application.Features.Media.Queries;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AiVideoStudio.UnitTests.Features.Media;

public class GetMediaByIdQueryHandlerTests
{
    private readonly IMediaAssetRepository _mediaAssetRepository = Substitute.For<IMediaAssetRepository>();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IMapper _mapper;

    private readonly GetMediaByIdQueryHandler _handler;

    public GetMediaByIdQueryHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MediaProfile>());
        _mapper = config.CreateMapper();

        _handler = new GetMediaByIdQueryHandler(
            _mediaAssetRepository,
            _projectRepository,
            _currentUser,
            _mapper);
    }

    [Fact]
    public async Task Handle_ShouldReturnMedia_WhenOwnerRequestsMedia()
    {
        // Arrange
        var ownerId = "owner_123";
        var project = Project.Create("Demo Project", ownerId);
        var mediaAsset = MediaAsset.Create(project.Id, ownerId, "file.jpg", "file.jpg", ".jpg", "image/jpeg", 1024, "path", AssetType.Image);
        var query = new GetMediaByIdQuery(mediaAsset.Id);

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(ownerId);
        _currentUser.Roles.Returns(new List<string> { "User" });

        _mediaAssetRepository.GetByIdAsync(mediaAsset.Id, Arg.Any<CancellationToken>()).Returns(mediaAsset);
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(mediaAsset.Id);
        result.Value.FileName.Should().Be("file.jpg");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenStrangerRequestsMedia()
    {
        // Arrange
        var ownerId = "owner_123";
        var strangerId = "stranger_456";
        var project = Project.Create("Demo Project", ownerId);
        var mediaAsset = MediaAsset.Create(project.Id, ownerId, "file.jpg", "file.jpg", ".jpg", "image/jpeg", 1024, "path", AssetType.Image);
        var query = new GetMediaByIdQuery(mediaAsset.Id);

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(strangerId);
        _currentUser.Roles.Returns(new List<string> { "User" });

        _mediaAssetRepository.GetByIdAsync(mediaAsset.Id, Arg.Any<CancellationToken>()).Returns(mediaAsset);
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(MediaErrors.UnauthorizedAccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenMediaNotFound()
    {
        // Arrange
        var query = new GetMediaByIdQuery("missing_id");

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns("user_1");

        _mediaAssetRepository.GetByIdAsync("missing_id", Arg.Any<CancellationToken>()).Returns((MediaAsset?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(MediaErrors.NotFound);
    }
}

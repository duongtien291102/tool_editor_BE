using AiVideoStudio.Application.Features.Media.Commands;
using AiVideoStudio.Application.Features.Media.Handlers;
using AiVideoStudio.Application.Features.Media.Mappings;
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

public class UpdateMediaCommandHandlerTests
{
    private readonly IMediaAssetRepository _mediaAssetRepository = Substitute.For<IMediaAssetRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IMapper _mapper;

    private readonly UpdateMediaCommandHandler _handler;

    public UpdateMediaCommandHandlerTests()
    {
        var config = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<MediaProfile>());
        _mapper = config.CreateMapper();

        _handler = new UpdateMediaCommandHandler(
            _mediaAssetRepository,
            _currentUser,
            _mapper);
    }

    [Fact]
    public async Task Handle_ShouldUpdateMetadataSuccessfully_WhenOwnerUpdatesMedia()
    {
        // Arrange
        var ownerId = "owner_123";
        var mediaAsset = MediaAsset.Create("proj_1", ownerId, "guid.jpg", "old.jpg", ".jpg", "image/jpeg", 1024, "path", AssetType.Image);
        var command = new UpdateMediaCommand(mediaAsset.Id, "new_name.jpg", 1920, 1080, null, "thumb.jpg");

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(ownerId);
        _currentUser.Roles.Returns(new List<string> { "User" });

        _mediaAssetRepository.GetByIdAsync(mediaAsset.Id, Arg.Any<CancellationToken>()).Returns(mediaAsset);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.FileName.Should().Be("new_name.jpg");
        result.Value.Width.Should().Be(1920);
        result.Value.Height.Should().Be(1080);
        result.Value.ThumbnailPath.Should().Be("thumb.jpg");

        await _mediaAssetRepository.Received(1).UpdateAsync(mediaAsset, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotOwnerNorAdmin()
    {
        // Arrange
        var ownerId = "owner_123";
        var strangerId = "stranger_456";
        var mediaAsset = MediaAsset.Create("proj_1", ownerId, "guid.jpg", "old.jpg", ".jpg", "image/jpeg", 1024, "path", AssetType.Image);
        var command = new UpdateMediaCommand(mediaAsset.Id, "hacked.jpg");

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(strangerId);
        _currentUser.Roles.Returns(new List<string> { "User" });

        _mediaAssetRepository.GetByIdAsync(mediaAsset.Id, Arg.Any<CancellationToken>()).Returns(mediaAsset);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(MediaErrors.UnauthorizedAccess);
        await _mediaAssetRepository.DidNotReceive().UpdateAsync(Arg.Any<MediaAsset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenMediaNotFound()
    {
        // Arrange
        var command = new UpdateMediaCommand("invalid_id", "name.jpg");

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns("user_1");

        _mediaAssetRepository.GetByIdAsync("invalid_id", Arg.Any<CancellationToken>()).Returns((MediaAsset?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(MediaErrors.NotFound);
    }
}

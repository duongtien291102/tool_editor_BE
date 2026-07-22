using AiVideoStudio.Application.Events;
using AiVideoStudio.Application.Features.Media.Commands;
using AiVideoStudio.Application.Features.Media.Handlers;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Application.Storage;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Events.Media;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using FluentAssertions;
using NSubstitute;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AiVideoStudio.UnitTests.Features.Media;

public class DeleteMediaCommandHandlerTests
{
    private readonly IMediaAssetRepository _mediaAssetRepository = Substitute.For<IMediaAssetRepository>();
    private readonly IStorageProvider _storageProvider = Substitute.For<IStorageProvider>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private readonly DeleteMediaCommandHandler _handler;

    public DeleteMediaCommandHandlerTests()
    {
        _handler = new DeleteMediaCommandHandler(
            _mediaAssetRepository,
            _storageProvider,
            _currentUser,
            _eventBus);
    }

    [Fact]
    public async Task Handle_ShouldSoftDeleteMedia_WhenOwnerDeletesMedia()
    {
        // Arrange
        var ownerId = "owner_123";
        var mediaAsset = MediaAsset.Create("proj_1", ownerId, "file.jpg", "file.jpg", ".jpg", "image/jpeg", 1024, "path", AssetType.Image);
        var command = new DeleteMediaCommand(mediaAsset.Id);

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(ownerId);
        _currentUser.Roles.Returns(new List<string> { "User" });

        _mediaAssetRepository.GetByIdAsync(mediaAsset.Id, Arg.Any<CancellationToken>()).Returns(mediaAsset);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        mediaAsset.IsDeleted.Should().BeTrue();
        mediaAsset.Status.Should().Be(MediaStatus.Deleted);

        await _mediaAssetRepository.Received(1).UpdateAsync(mediaAsset, Arg.Any<CancellationToken>());
        await _storageProvider.Received(1).DeleteAsync("proj_1", "file.jpg", Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(Arg.Is<MediaDeletedEvent>(e => e.MediaId == mediaAsset.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotOwnerNorAdmin()
    {
        // Arrange
        var ownerId = "owner_123";
        var strangerId = "stranger_456";
        var mediaAsset = MediaAsset.Create("proj_1", ownerId, "file.jpg", "file.jpg", ".jpg", "image/jpeg", 1024, "path", AssetType.Image);
        var command = new DeleteMediaCommand(mediaAsset.Id);

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(strangerId);
        _currentUser.Roles.Returns(new List<string> { "User" });

        _mediaAssetRepository.GetByIdAsync(mediaAsset.Id, Arg.Any<CancellationToken>()).Returns(mediaAsset);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(MediaErrors.UnauthorizedAccess);
        mediaAsset.IsDeleted.Should().BeFalse();
        await _mediaAssetRepository.DidNotReceive().UpdateAsync(Arg.Any<MediaAsset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenMediaNotFound()
    {
        // Arrange
        var command = new DeleteMediaCommand("invalid_id");

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
